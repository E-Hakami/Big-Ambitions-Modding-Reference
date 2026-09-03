using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using Entities;
using Entities.Employee.JobDemands;
using Entities.Employee.JobDemands.Requirements;
using Enums;
using Google.OrTools.Sat;
using Helpers;
using UnityEngine;
using UnityEngine.Events;

namespace Buildings.Schedule;

public class ScheduleAutoFiller
{
	private struct PartitionProcessOptions
	{
		public bool skipOptional;

		public bool skipSemiCritical;

		public bool skipOptionalForDayFill;
	}

	private const int MaxEmployeeHoursPerDay = 12;

	private const float MaxSecondsPerSolverRun = 5f;

	private const float MaxSecondsPerPartition = 9f;

	private const int SolverWorkers = 3;

	private const float MinAcceptableShiftsRatioAtFirstPass = 0.75f;

	private static readonly int[] MaxHourDeviationSteps = new int[3] { -1, 6, 1 };

	public readonly HashSet<(DayOfWeekOrdered day, int hour, string workstationId, string employeeId)> firstPassValues = new HashSet<(DayOfWeekOrdered, int, string, string)>();

	public readonly UnityEvent<ScheduleAutoFiller, float> onProgress = new UnityEvent<ScheduleAutoFiller, float>();

	public readonly UnityEvent<ScheduleAutoFiller, bool> onCompleted = new UnityEvent<ScheduleAutoFiller, bool>();

	public bool isOptimal = true;

	public bool skipOptionalForDayFill;

	public bool inhibitSuccessNotification;

	public bool fast;

	private readonly List<WorkStationInfo> _workStations;

	private readonly List<EmployeeInstance> _employees;

	private readonly SchedulePartitioner _partitioner;

	private readonly Dictionary<ScheduleDay, List<WorkShift>> _resultShifts = new Dictionary<ScheduleDay, List<WorkShift>>();

	private readonly Dictionary<ScheduleDay, List<WorkShift>> _partitionResultShifts = new Dictionary<ScheduleDay, List<WorkShift>>();

	private readonly CpSolver _solver;

	private readonly ScheduleDay _specificDayToFill;

	private readonly List<ScheduleDay> _openDays;

	private readonly List<ScheduleDay> _modifiableDays;

	private readonly HashSet<string> _unsolvedWorkStationIds = new HashSet<string>();

	private readonly List<(ScheduleDay scheduleDay, int hour)> _shiftStarts = new List<(ScheduleDay, int)>();

	private int _partitionResultShiftsCount;

	private double _partitionElapsedTime;

	private bool _cancelRequested;

	public CpModel Model { get; private set; }

	public Dictionary<(DayOfWeekOrdered day, int hour, string workstationId, string employeeId), IntVar> ScheduleDictionary { get; } = new Dictionary<(DayOfWeekOrdered, int, string, string), IntVar>();

	public bool IsSecondPass { get; private set; }

	public Dictionary<(DayOfWeekOrdered day, string employeeId), IntVar> EmployeeDayDictionary { get; } = new Dictionary<(DayOfWeekOrdered, string), IntVar>();

	public BuildingRegistration Registration { get; }

	public bool IsSingleDay => _specificDayToFill != null;

	public float CurrentProgress { get; private set; }

	public bool Invalidated { get; private set; }

	public bool SkippingSemiCriticalDemands { get; private set; }

	public List<EmployeeInstance> Employees => _employees;

	public List<EmployeeInstance> UnassignedEmployees => _partitioner.unassignedEmployees;

	private List<PartitionProcessOptions> PartitionOptionsSteps
	{
		get
		{
			if (_specificDayToFill != null)
			{
				return new List<PartitionProcessOptions>
				{
					default(PartitionProcessOptions),
					new PartitionProcessOptions
					{
						skipOptionalForDayFill = true
					},
					new PartitionProcessOptions
					{
						skipOptionalForDayFill = true,
						skipOptional = true
					},
					new PartitionProcessOptions
					{
						skipOptionalForDayFill = true,
						skipOptional = true,
						skipSemiCritical = true
					}
				};
			}
			return new List<PartitionProcessOptions>
			{
				default(PartitionProcessOptions),
				new PartitionProcessOptions
				{
					skipOptional = true
				},
				new PartitionProcessOptions
				{
					skipOptional = true,
					skipSemiCritical = true
				}
			};
		}
	}

	public ScheduleAutoFiller(List<EmployeeInstance> employees, BuildingRegistration buildingRegistration, ScheduleDay specificDayToFill = null)
	{
		_employees = employees;
		Registration = buildingRegistration;
		_specificDayToFill = specificDayToFill;
		_workStations = GetWorkStations();
		_partitioner = new SchedulePartitioner(_employees, _workStations, Registration, _specificDayToFill);
		_solver = new CpSolver
		{
			StringParameters = $"max_time_in_seconds:{5f} num_workers:{3}"
		};
		if (_partitioner.unsatisfiedEmployees.Count > 0)
		{
			isOptimal = false;
		}
		_openDays = new List<ScheduleDay>();
		foreach (ScheduleDay scheduleDay in Registration.scheduleDays)
		{
			if (scheduleDay.isOpen)
			{
				_openDays.Add(scheduleDay);
			}
		}
		_modifiableDays = new List<ScheduleDay>();
		foreach (ScheduleDay scheduleDay2 in Registration.scheduleDays)
		{
			if (IsModifiableDay(scheduleDay2))
			{
				_modifiableDays.Add(scheduleDay2);
			}
		}
		ClearStoredShifts();
	}

	private void ClearStoredShifts()
	{
		foreach (ScheduleDay modifiableDay in _modifiableDays)
		{
			_resultShifts[modifiableDay] = new List<WorkShift>();
			_partitionResultShifts[modifiableDay] = new List<WorkShift>();
		}
	}

	private List<WorkStationInfo> GetWorkStations()
	{
		List<ItemInstance> assignableItems = Registration.GetAssignableItems();
		assignableItems.Sort((ItemInstance x, ItemInstance y) => x.itemName.CompareTo(y.itemName));
		List<WorkStationInfo> list = new List<WorkStationInfo>();
		foreach (ItemInstance item in assignableItems)
		{
			list.Add(GetWorkStationInfo(item));
		}
		return list;
	}

	private static WorkStationInfo GetWorkStationInfo(ItemInstance workStation)
	{
		Item itemCached = workStation.ItemCached;
		bool flag = itemCached.suitableSkills != null && itemCached.suitableSkills.Contains("ba:skill_cleaning");
		return new WorkStationInfo
		{
			id = workStation.id,
			itemInstance = workStation,
			skillsRequired = itemCached.suitableSkills,
			workShiftType = ((!flag) ? WorkShiftType.Default : WorkShiftType.Cleaning)
		};
	}

	public void FillWithEmployees()
	{
		int num = 0;
		foreach (SchedulePartitioner.SchedulePartition partition in _partitioner.partitions)
		{
			if (CheckShouldAbort())
			{
				return;
			}
			num++;
			CurrentProgress = (float)num / (float)_partitioner.partitions.Count / 2f;
			GameManager.EnqueueMainThreadAction(delegate
			{
				onProgress.Invoke(this, CurrentProgress);
			});
			if (ProcessPartitionWithSteps(partition, PartitionOptionsSteps))
			{
				continue;
			}
			foreach (WorkStationInfo workStation in partition.workStations)
			{
				_unsolvedWorkStationIds.Add(workStation.id);
			}
		}
		Invalidated = Invalidated || !IsInitialDataValid();
		if (!CheckShouldAbort())
		{
			if (Registration.businessTypeName == "ba:businesstype_headquarters")
			{
				ApplyResult();
			}
			else
			{
				FillSecondPass();
			}
		}
	}

	private void FillSecondPass()
	{
		SaveFirstPassResult();
		ClearStoredShifts();
		IsSecondPass = true;
		_partitioner.CalculateSecondPass();
		int num = 0;
		foreach (SchedulePartitioner.SchedulePartition partition in _partitioner.partitions)
		{
			if (CheckShouldAbort())
			{
				return;
			}
			num++;
			CurrentProgress = 0.5f + (float)num / (float)_partitioner.partitions.Count / 2f;
			GameManager.EnqueueMainThreadAction(delegate
			{
				onProgress.Invoke(this, CurrentProgress);
			});
			_partitionResultShiftsCount = 0;
			_partitionElapsedTime = 0.0;
			foreach (PartitionProcessOptions partitionOptionsStep in PartitionOptionsSteps)
			{
				skipOptionalForDayFill = _specificDayToFill != null && partitionOptionsStep.skipOptionalForDayFill;
				if (ProcessPartition(partition, partitionOptionsStep, -1))
				{
					StorePartitionResult();
					break;
				}
				if (_cancelRequested || Invalidated)
				{
					break;
				}
			}
		}
		Invalidated = Invalidated || !IsInitialDataValid();
		if (!CheckShouldAbort())
		{
			ApplyResult();
		}
	}

	private bool ProcessPartitionWithSteps(SchedulePartitioner.SchedulePartition partition, IEnumerable<PartitionProcessOptions> steps)
	{
		foreach (PartitionProcessOptions step in steps)
		{
			skipOptionalForDayFill = _specificDayToFill != null && step.skipOptionalForDayFill;
			if (step.skipOptional)
			{
				isOptimal = false;
			}
			if (ProcessPartitionWithHourDeviationSteps(partition, step))
			{
				return true;
			}
			if (_cancelRequested || Invalidated)
			{
				return true;
			}
		}
		return false;
	}

	private bool ProcessPartitionWithHourDeviationSteps(SchedulePartitioner.SchedulePartition partition, PartitionProcessOptions options)
	{
		_partitionResultShiftsCount = 0;
		_partitionElapsedTime = 0.0;
		bool flag = false;
		int[] maxHourDeviationSteps = MaxHourDeviationSteps;
		foreach (int maxHourDeviation in maxHourDeviationSteps)
		{
			if (!IsInitialDataValid())
			{
				Invalidated = true;
				return true;
			}
			if (ProcessPartition(partition, options, maxHourDeviation))
			{
				flag = true;
			}
			if (_cancelRequested)
			{
				return true;
			}
			if (_partitionElapsedTime > 9.0)
			{
				break;
			}
		}
		if (flag)
		{
			StorePartitionResult();
		}
		return flag;
	}

	private bool ProcessPartition(SchedulePartitioner.SchedulePartition partition, PartitionProcessOptions options, int maxHourDeviation)
	{
		Model = new CpModel();
		DefineVariables(partition);
		if (ScheduleDictionary.Count == 0)
		{
			return true;
		}
		AddConstraints(partition, options, maxHourDeviation);
		DefineObjective(partition);
		CpSolverStatus cpSolverStatus = _solver.Solve(Model);
		_partitionElapsedTime += _solver.WallTime();
		int num;
		if (cpSolverStatus != CpSolverStatus.Optimal)
		{
			num = ((cpSolverStatus == CpSolverStatus.Feasible) ? 1 : 0);
			if (num == 0)
			{
				goto IL_0077;
			}
		}
		else
		{
			num = 1;
		}
		ReadSolution(_solver, partition);
		goto IL_0077;
		IL_0077:
		return (byte)num != 0;
	}

	private void DefineVariables(SchedulePartitioner.SchedulePartition partition)
	{
		ScheduleDictionary.Clear();
		EmployeeDayDictionary.Clear();
		DefineWorkShiftVariables(partition);
		DefineWorkDayVariables(partition);
	}

	private void DefineWorkShiftVariables(SchedulePartitioner.SchedulePartition partition)
	{
		foreach (ScheduleDay openDay in _openDays)
		{
			bool flag = IsModifiableDay(openDay);
			foreach (WorkStationInfo workStation in partition.workStations)
			{
				foreach (OpeningHourSlot openingHourSlot in openDay.openingHourSlots)
				{
					for (int i = openingHourSlot.startingHour; i < openingHourSlot.endingHour; i++)
					{
						foreach (EmployeeInstance item in _partitioner.suitableEmployees[workStation])
						{
							string name = $"{openDay.day}_{i}_{workStation.id}_{item.id}";
							BoolVar boolVar = Model.NewBoolVar(name);
							(DayOfWeekOrdered, int, string, string) tuple = (openDay.day, i, workStation.id, item.id);
							ScheduleDictionary[tuple] = boolVar;
							if (flag)
							{
								if (IsSecondPass && firstPassValues.Contains(tuple))
								{
									Model.Add(boolVar == 1L);
								}
							}
							else
							{
								bool flag2 = EmployeeWorksAtStationDayHour(item, workStation, openDay.day, i);
								Model.Add(boolVar == (flag2 ? 1 : 0));
							}
						}
					}
				}
			}
		}
	}

	private void DefineWorkDayVariables(SchedulePartitioner.SchedulePartition partition)
	{
		List<IntVar> list = new List<IntVar>();
		foreach (ScheduleDay openDay in _openDays)
		{
			foreach (EmployeeInstance employee in _employees)
			{
				list.Clear();
				foreach (WorkStationInfo workStation in partition.workStations)
				{
					foreach (OpeningHourSlot openingHourSlot in openDay.openingHourSlots)
					{
						for (int i = openingHourSlot.startingHour; i < openingHourSlot.endingHour; i++)
						{
							(DayOfWeekOrdered, int, string, string) key = (openDay.day, i, workStation.id, employee.id);
							if (ScheduleDictionary.TryGetValue(key, out var value))
							{
								list.Add(value);
							}
						}
					}
				}
				if (list.Count != 0)
				{
					string name = $"works_{employee.id}_{openDay.day}";
					BoolVar boolVar = Model.NewBoolVar(name);
					EmployeeDayDictionary[(openDay.day, employee.id)] = boolVar;
					Model.AddMaxEquality(boolVar, list.ToArray());
				}
			}
		}
	}

	private void AddConstraints(SchedulePartitioner.SchedulePartition partition, PartitionProcessOptions options, int maxHourDeviation)
	{
		AddMaxOneEmployeePerWorkstationConstraint(partition);
		AddMaxOneShiftPerEmployeeConstraint(partition);
		AddConsecutiveShiftsConstraint(partition);
		AddEmployeeMaxHoursPerDayConstraint(partition);
		AddHeadquartersConstraint(partition);
		AddMaxCleaningHoursPerDayConstraint(partition);
		AddEvenHoursDistributionConstraint(partition, maxHourDeviation);
		AddEmployeeDemandsConstraints(partition, options);
	}

	private void AddMaxOneEmployeePerWorkstationConstraint(SchedulePartitioner.SchedulePartition partition)
	{
		List<IntVar> list = new List<IntVar>();
		foreach (ScheduleDay modifiableDay in _modifiableDays)
		{
			foreach (WorkStationInfo workStation in partition.workStations)
			{
				foreach (OpeningHourSlot openingHourSlot in modifiableDay.openingHourSlots)
				{
					for (int i = openingHourSlot.startingHour; i < openingHourSlot.endingHour; i++)
					{
						list.Clear();
						foreach (EmployeeInstance item in _partitioner.suitableEmployees[workStation])
						{
							list.Add(ScheduleDictionary[(modifiableDay.day, i, workStation.id, item.id)]);
						}
						if (list.Count > 1)
						{
							Model.Add(LinearExpr.Sum(list.ToArray()) <= 1L);
						}
					}
				}
			}
		}
	}

	private void AddMaxOneShiftPerEmployeeConstraint(SchedulePartitioner.SchedulePartition partition)
	{
		List<IntVar> list = new List<IntVar>();
		HashSet<string> hashSet = new HashSet<string>();
		foreach (EmployeeInstance employee in _employees)
		{
			foreach (ScheduleDay modifiableDay in _modifiableDays)
			{
				foreach (OpeningHourSlot openingHourSlot in modifiableDay.openingHourSlots)
				{
					for (int i = openingHourSlot.startingHour; i < openingHourSlot.endingHour; i++)
					{
						list.Clear();
						hashSet.Clear();
						foreach (WorkStationInfo workStation in partition.workStations)
						{
							if (_partitioner.suitableEmployees[workStation].Contains(employee))
							{
								list.Add(ScheduleDictionary[(modifiableDay.day, i, workStation.id, employee.id)]);
								hashSet.Add(workStation.id);
							}
						}
						if (list.Count == 0)
						{
							continue;
						}
						bool flag = false;
						if (IsSecondPass)
						{
							foreach (var (dayOfWeekOrdered, num, item, text) in firstPassValues)
							{
								if (dayOfWeekOrdered == modifiableDay.day && num == i && !(text != employee.id) && !hashSet.Contains(item))
								{
									flag = true;
									break;
								}
							}
						}
						if (flag)
						{
							Model.Add(LinearExpr.Sum(list.ToArray()) == 0L);
						}
						else if (list.Count > 1)
						{
							Model.Add(LinearExpr.Sum(list.ToArray()) <= 1L);
						}
					}
				}
			}
		}
	}

	private void AddConsecutiveShiftsConstraint(SchedulePartitioner.SchedulePartition partition)
	{
		_shiftStarts.Clear();
		foreach (ScheduleDay modifiableDay in _modifiableDays)
		{
			int minHour = 0;
			(int, int) nextContinuousOpenHours = modifiableDay.GetNextContinuousOpenHours(minHour);
			while (nextContinuousOpenHours.Item1 >= 0)
			{
				minHour = nextContinuousOpenHours.Item2;
				if (nextContinuousOpenHours.Item2 - nextContinuousOpenHours.Item1 <= 1)
				{
					nextContinuousOpenHours = modifiableDay.GetNextContinuousOpenHours(minHour);
					continue;
				}
				int num = nextContinuousOpenHours.Item2 - nextContinuousOpenHours.Item1;
				int consecutiveShiftHours = GetConsecutiveShiftHours(num, partition.expectedHourCoverage);
				for (int i = 1; i < num; i++)
				{
					if (i % consecutiveShiftHours == 0 && i < num - 1)
					{
						continue;
					}
					int num2 = nextContinuousOpenHours.Item1 + i;
					if (i % consecutiveShiftHours == 1)
					{
						_shiftStarts.Add((modifiableDay, num2 - 1));
					}
					foreach (WorkStationInfo workStation in partition.workStations)
					{
						if (Registration.businessTypeName == "ba:businesstype_headquarters" && workStation.workShiftType != WorkShiftType.Cleaning)
						{
							continue;
						}
						foreach (EmployeeInstance item in _partitioner.suitableEmployees[workStation])
						{
							if (ScheduleDictionary.TryGetValue((modifiableDay.day, num2, workStation.id, item.id), out var value) && ScheduleDictionary.TryGetValue((modifiableDay.day, num2 - 1, workStation.id, item.id), out var value2))
							{
								Model.Add(value == value2);
							}
						}
					}
				}
				nextContinuousOpenHours = modifiableDay.GetNextContinuousOpenHours(minHour);
			}
		}
	}

	private int GetConsecutiveShiftHours(int totalHours, float expectedHourCoverage)
	{
		int num = 1;
		int num2 = totalHours;
		int idealShiftConsecutiveHours = GetIdealShiftConsecutiveHours(totalHours, expectedHourCoverage);
		while (num2 > idealShiftConsecutiveHours && num < totalHours)
		{
			num++;
			num2 = totalHours / num;
		}
		return num2;
	}

	private int GetIdealShiftConsecutiveHours(int slotHours, float expectedHourCoverage)
	{
		if (slotHours < 9 || expectedHourCoverage > 0.75f)
		{
			return 2;
		}
		if (slotHours < 16)
		{
			return 3;
		}
		if (!IsSecondPass)
		{
			return 4;
		}
		return 2;
	}

	private void AddHeadquartersConstraint(SchedulePartitioner.SchedulePartition partition)
	{
		if (Registration.businessTypeName != "ba:businesstype_headquarters")
		{
			return;
		}
		List<IntVar> list = new List<IntVar>();
		foreach (WorkStationInfo workStation in partition.workStations)
		{
			if (workStation.workShiftType == WorkShiftType.Cleaning)
			{
				continue;
			}
			foreach (EmployeeInstance item in _partitioner.suitableEmployees[workStation])
			{
				list.Clear();
				foreach (ScheduleDay modifiableDay in _modifiableDays)
				{
					bool flag = false;
					foreach (OpeningHourSlot openingHourSlot in modifiableDay.openingHourSlots)
					{
						for (int i = openingHourSlot.startingHour; i < openingHourSlot.endingHour; i++)
						{
							if (!ScheduleDictionary.TryGetValue((modifiableDay.day, i, workStation.id, item.id), out var value) || !ScheduleDictionary.TryGetValue((modifiableDay.day, i - 1, workStation.id, item.id), out var value2))
							{
								continue;
							}
							Model.Add(value == value2);
							if (flag)
							{
								continue;
							}
							flag = true;
							foreach (IntVar item2 in list)
							{
								Model.Add(item2 == value2);
							}
							list.Add(value);
						}
					}
				}
			}
		}
	}

	private void AddEmployeeMaxHoursPerDayConstraint(SchedulePartitioner.SchedulePartition partition)
	{
		List<IntVar> list = new List<IntVar>();
		foreach (EmployeeInstance employee in _employees)
		{
			foreach (ScheduleDay modifiableDay in _modifiableDays)
			{
				list.Clear();
				foreach (OpeningHourSlot openingHourSlot in modifiableDay.openingHourSlots)
				{
					for (int i = openingHourSlot.startingHour; i < openingHourSlot.endingHour; i++)
					{
						foreach (WorkStationInfo workStation in partition.workStations)
						{
							if (_partitioner.suitableEmployees[workStation].Contains(employee))
							{
								list.Add(ScheduleDictionary[(modifiableDay.day, i, workStation.id, employee.id)]);
							}
						}
					}
				}
				if (list.Count > 12)
				{
					Model.Add(LinearExpr.Sum(list.ToArray()) <= 12L);
				}
			}
		}
	}

	private void AddMaxCleaningHoursPerDayConstraint(SchedulePartitioner.SchedulePartition partition)
	{
		Dictionary<ScheduleDay, int> maxCleaningHoursPerDay = GetMaxCleaningHoursPerDay(partition);
		if (maxCleaningHoursPerDay == null)
		{
			return;
		}
		List<IntVar> list = new List<IntVar>();
		foreach (ScheduleDay modifiableDay in _modifiableDays)
		{
			list.Clear();
			int num = maxCleaningHoursPerDay[modifiableDay];
			foreach (WorkStationInfo workStation in partition.workStations)
			{
				if (workStation.workShiftType != WorkShiftType.Cleaning)
				{
					continue;
				}
				foreach (EmployeeInstance item in _partitioner.suitableEmployees[workStation])
				{
					foreach (OpeningHourSlot openingHourSlot in modifiableDay.openingHourSlots)
					{
						for (int i = openingHourSlot.startingHour; i < openingHourSlot.endingHour; i++)
						{
							list.Add(ScheduleDictionary[(modifiableDay.day, i, workStation.id, item.id)]);
						}
					}
				}
			}
			if (list.Count > num)
			{
				Model.Add(LinearExpr.Sum(list.ToArray()) <= num);
			}
		}
	}

	private void AddEvenHoursDistributionConstraint(SchedulePartitioner.SchedulePartition partition, int maxHourDeviation)
	{
		if (maxHourDeviation < 0)
		{
			return;
		}
		Dictionary<EmployeeInstance, List<IntVar>> dictionary = new Dictionary<EmployeeInstance, List<IntVar>>();
		foreach (ScheduleDay modifiableDay in _modifiableDays)
		{
			foreach (EmployeeInstance employee in _employees)
			{
				List<IntVar> list = (dictionary[employee] = new List<IntVar>());
				foreach (WorkStationInfo workStation in partition.workStations)
				{
					if (!_partitioner.suitableEmployees[workStation].Contains(employee))
					{
						continue;
					}
					foreach (OpeningHourSlot openingHourSlot in modifiableDay.openingHourSlots)
					{
						for (int i = openingHourSlot.startingHour; i < openingHourSlot.endingHour; i++)
						{
							if (ScheduleDictionary.TryGetValue((modifiableDay.day, i, workStation.id, employee.id), out var value))
							{
								list.Add(value);
							}
						}
					}
				}
			}
		}
		for (int j = 0; j < _employees.Count; j++)
		{
			for (int k = j + 1; k < _employees.Count; k++)
			{
				EmployeeInstance key = _employees[j];
				EmployeeInstance key2 = _employees[k];
				List<IntVar> list3 = dictionary[key];
				List<IntVar> list4 = dictionary[key2];
				if (list3.Count != 0 && list4.Count != 0)
				{
					LinearExpr linearExpr = LinearExpr.Sum(list3.ToArray());
					LinearExpr linearExpr2 = LinearExpr.Sum(list4.ToArray());
					Model.Add(linearExpr <= linearExpr2 + maxHourDeviation);
					Model.Add(linearExpr2 <= linearExpr + maxHourDeviation);
				}
			}
		}
	}

	private void AddEmployeeDemandsConstraints(SchedulePartitioner.SchedulePartition partition, PartitionProcessOptions options)
	{
		SkippingSemiCriticalDemands = options.skipSemiCritical;
		foreach (EmployeeInstance employee in _employees)
		{
			bool flag = false;
			foreach (string demand in employee.demands)
			{
				JobDemand byName = JobDemandHelper.GetByName(demand);
				if (byName is IScheduleConstraint scheduleConstraint && (!options.skipOptional || byName.priority == Priority.High))
				{
					scheduleConstraint.ApplyConstraint(this, employee, partition.workStations);
					if (byName is HoursWorkingPerWeek)
					{
						flag = true;
					}
				}
			}
			if (!flag)
			{
				HoursWorkingPerWeek.ApplyConstraintUsing(this, employee, partition.workStations, new Vector2Int(1, -1));
			}
		}
	}

	private void DefineObjective(SchedulePartitioner.SchedulePartition partition)
	{
		List<IntVar> list = OccupiedWorkstationsObjective(partition);
		if (list.Count != 0)
		{
			if (!fast)
			{
				ConsecutiveShiftsObjective(partition, list);
			}
			_shiftStarts.Clear();
			Model.Maximize(LinearExpr.Sum(list));
		}
	}

	private List<IntVar> OccupiedWorkstationsObjective(SchedulePartitioner.SchedulePartition partition)
	{
		List<IntVar> list = new List<IntVar>();
		foreach (ScheduleDay modifiableDay in _modifiableDays)
		{
			foreach (OpeningHourSlot openingHourSlot in modifiableDay.openingHourSlots)
			{
				for (int i = openingHourSlot.startingHour; i < openingHourSlot.endingHour; i++)
				{
					foreach (WorkStationInfo workStation in partition.workStations)
					{
						foreach (EmployeeInstance item in _partitioner.suitableEmployees[workStation])
						{
							list.Add(ScheduleDictionary[(modifiableDay.day, i, workStation.id, item.id)]);
						}
					}
				}
			}
		}
		return list;
	}

	private void ConsecutiveShiftsObjective(SchedulePartitioner.SchedulePartition partition, List<IntVar> appendTo)
	{
		foreach (WorkStationInfo workStation in partition.workStations)
		{
			if (Registration.businessTypeName == "ba:businesstype_headquarters" && workStation.workShiftType != WorkShiftType.Cleaning)
			{
				continue;
			}
			foreach (var (scheduleDay, num) in _shiftStarts)
			{
				foreach (EmployeeInstance item in _partitioner.suitableEmployees[workStation])
				{
					if (ScheduleDictionary.TryGetValue((scheduleDay.day, num, workStation.id, item.id), out var value) && ScheduleDictionary.TryGetValue((scheduleDay.day, num - 1, workStation.id, item.id), out var value2))
					{
						BoolVar boolVar = Model.NewBoolVar($"consecutive_{scheduleDay.day}_{num}_{workStation.id}_{item.id}");
						Model.AddMinEquality(boolVar, new IntVar[2] { value, value2 });
						appendTo.Add(boolVar);
					}
				}
			}
		}
	}

	private void ReadSolution(CpSolver solver, SchedulePartitioner.SchedulePartition partition)
	{
		int solutionShiftsCount = GetSolutionShiftsCount(solver, partition);
		if (IsSecondPass)
		{
			if (solutionShiftsCount <= _partitionResultShiftsCount)
			{
				return;
			}
		}
		else if ((float)solutionShiftsCount < (float)_partitionResultShiftsCount * 0.75f)
		{
			return;
		}
		_partitionResultShiftsCount = Mathf.Max(_partitionResultShiftsCount, solutionShiftsCount);
		foreach (ScheduleDay modifiableDay in _modifiableDays)
		{
			_partitionResultShifts[modifiableDay].Clear();
			foreach (WorkStationInfo workStation in partition.workStations)
			{
				foreach (EmployeeInstance item in _partitioner.suitableEmployees[workStation])
				{
					WorkShift workShift = null;
					for (int i = 0; i < 24; i++)
					{
						(DayOfWeekOrdered, int, string, string) key = (modifiableDay.day, i, workStation.id, item.id);
						if (!ScheduleDictionary.TryGetValue(key, out var value) || solver.Value(value) != 1)
						{
							workShift = null;
						}
						else if (workShift == null)
						{
							workShift = new WorkShift
							{
								employeeId = item.id,
								itemInstanceId = workStation.id,
								startingHour = i,
								endingHour = i + 1,
								type = workStation.workShiftType
							};
							_partitionResultShifts[modifiableDay].Add(workShift);
						}
						else
						{
							workShift.endingHour++;
						}
					}
				}
			}
		}
	}

	private void StorePartitionResult()
	{
		foreach (ScheduleDay modifiableDay in _modifiableDays)
		{
			foreach (WorkShift item in _partitionResultShifts[modifiableDay])
			{
				_resultShifts[modifiableDay].Add(item);
				AddShiftToFirstPassValues(modifiableDay, item);
			}
			_partitionResultShifts[modifiableDay].Clear();
		}
	}

	private void ApplyResult()
	{
		if (_specificDayToFill == null)
		{
			ClearClosedDays();
		}
		foreach (ScheduleDay modifiableDay in _modifiableDays)
		{
			modifiableDay.RemoveAllWorkShiftsThatMatchPredicate((WorkShift x) => !_unsolvedWorkStationIds.Contains(x.itemInstanceId));
			if (!_resultShifts.TryGetValue(modifiableDay, out var value))
			{
				continue;
			}
			foreach (WorkShift item in value)
			{
				modifiableDay.AddWorkShift(item);
			}
		}
		foreach (EmployeeInstance employee in _employees)
		{
			employee.UpdateWeeklyHoursAndDays();
			employee.UpdateAssignedWorkStationItems();
		}
		SafeComplete(successful: true);
	}

	private void ClearClosedDays()
	{
		foreach (ScheduleDay scheduleDay in Registration.scheduleDays)
		{
			if (!scheduleDay.isOpen)
			{
				scheduleDay.ClearWorkShifts();
			}
		}
	}

	public void RequestCancel()
	{
		if (!_cancelRequested)
		{
			_cancelRequested = true;
			_solver.StopSearch();
		}
	}

	private bool CheckShouldAbort()
	{
		if (!_cancelRequested && !Invalidated)
		{
			return false;
		}
		SafeComplete(successful: false);
		return true;
	}

	private void SafeComplete(bool successful)
	{
		GameManager.EnqueueMainThreadAction(delegate
		{
			onCompleted.Invoke(this, successful);
			onCompleted.RemoveAllListeners();
			onProgress.RemoveAllListeners();
		});
	}

	private bool IsModifiableDay(ScheduleDay scheduleDay)
	{
		if (scheduleDay.isOpen)
		{
			if (_specificDayToFill != null)
			{
				return scheduleDay == _specificDayToFill;
			}
			return true;
		}
		return false;
	}

	private bool EmployeeWorksAtStationDayHour(EmployeeInstance employee, WorkStationInfo workstation, DayOfWeekOrdered day, int hour)
	{
		ScheduleDay scheduleDay = null;
		foreach (ScheduleDay scheduleDay2 in Registration.scheduleDays)
		{
			if (scheduleDay2.day == day)
			{
				scheduleDay = scheduleDay2;
				break;
			}
		}
		if (scheduleDay == null || !scheduleDay.isOpen)
		{
			return false;
		}
		foreach (WorkShift workShift in scheduleDay.workShifts)
		{
			if (workShift.employeeId == employee.id && workShift.itemInstanceId == workstation.id && workShift.startingHour <= hour && workShift.endingHour > hour)
			{
				return true;
			}
		}
		return false;
	}

	private int GetSolutionShiftsCount(CpSolver solver, SchedulePartitioner.SchedulePartition partition)
	{
		int num = 0;
		foreach (ScheduleDay modifiableDay in _modifiableDays)
		{
			foreach (WorkStationInfo workStation in partition.workStations)
			{
				foreach (OpeningHourSlot openingHourSlot in modifiableDay.openingHourSlots)
				{
					for (int i = openingHourSlot.startingHour; i < openingHourSlot.endingHour; i++)
					{
						foreach (EmployeeInstance item in _partitioner.suitableEmployees[workStation])
						{
							(DayOfWeekOrdered, int, string, string) key = (modifiableDay.day, i, workStation.id, item.id);
							if (solver.Value(ScheduleDictionary[key]) == 1)
							{
								num++;
								break;
							}
						}
					}
				}
			}
		}
		return num;
	}

	private void SaveFirstPassResult()
	{
		foreach (ScheduleDay modifiableDay in _modifiableDays)
		{
			foreach (WorkShift item in _resultShifts[modifiableDay])
			{
				AddShiftToFirstPassValues(modifiableDay, item);
			}
		}
	}

	private void AddShiftToFirstPassValues(ScheduleDay scheduleDay, WorkShift shift)
	{
		for (int i = shift.startingHour; i < shift.endingHour; i++)
		{
			(DayOfWeekOrdered, int, string, string) item = (scheduleDay.day, i, shift.itemInstanceId, shift.employeeId);
			firstPassValues.Add(item);
		}
	}

	private bool IsInitialDataValid()
	{
		foreach (EmployeeInstance employee in _employees)
		{
			if (BuildingHelper.GetBuildingRegistration(employee.assignedAddress) != Registration)
			{
				return false;
			}
		}
		List<ItemInstance> assignableItems = Registration.GetAssignableItems();
		foreach (WorkStationInfo workStation in _workStations)
		{
			if (!assignableItems.Contains(workStation.itemInstance))
			{
				return false;
			}
		}
		return true;
	}

	private Dictionary<ScheduleDay, int> GetMaxCleaningHoursPerDay(SchedulePartitioner.SchedulePartition partition)
	{
		if (partition.maxCleaningHours <= 0)
		{
			return null;
		}
		Dictionary<ScheduleDay, int> dictionary = new Dictionary<ScheduleDay, int>();
		int num = 0;
		foreach (ScheduleDay modifiableDay in _modifiableDays)
		{
			int num2 = (dictionary[modifiableDay] = modifiableDay.GetHoursOpen);
			num += num2;
		}
		if (num <= partition.maxCleaningHours)
		{
			return null;
		}
		float num3 = (float)partition.maxCleaningHours / (float)num;
		foreach (ScheduleDay modifiableDay2 in _modifiableDays)
		{
			dictionary[modifiableDay2] = Mathf.CeilToInt((float)dictionary[modifiableDay2] * num3);
		}
		return dictionary;
	}
}
