// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BAModAPI.Services.ServiceHelper
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class ServiceHelper
{
	private static SynchronizationContext UnityContext;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		UnityContext = null;
	}

	private static void Initialize()
	{
		if (UnityContext == null)
		{
			if (SynchronizationContext.Current == null)
			{
				throw new InvalidOperationException("AssetService.Initialize must be called from Unity main thread.");
			}
			UnityContext = SynchronizationContext.Current;
		}
	}

	public static void EnsureInitialized()
	{
		if (UnityContext == null)
		{
			Initialize();
		}
	}

	internal static Task<T> RunOnMainThreadAsync<T>(Func<T> function)
	{
		EnsureInitialized();
		if (SynchronizationContext.Current == UnityContext)
		{
			try
			{
				return Task.FromResult(function());
			}
			catch (Exception exception)
			{
				return Task.FromException<T>(exception);
			}
		}
		TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		UnityContext.Post(delegate
		{
			try
			{
				taskCompletionSource.TrySetResult(function());
			}
			catch (Exception exception2)
			{
				taskCompletionSource.TrySetException(exception2);
			}
		}, null);
		return taskCompletionSource.Task;
	}

	internal static Task<T> RunOnMainThreadAsync<T>(Func<Task<T>> function)
	{
		EnsureInitialized();
		if (SynchronizationContext.Current == UnityContext)
		{
			try
			{
				return function();
			}
			catch (Exception exception)
			{
				return Task.FromException<T>(exception);
			}
		}
		TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		UnityContext.Post(async delegate
		{
			try
			{
				TaskCompletionSource<T> taskCompletionSource2 = taskCompletionSource;
				taskCompletionSource2.TrySetResult(await function().ConfigureAwait(continueOnCapturedContext: false));
			}
			catch (Exception exception2)
			{
				taskCompletionSource.TrySetException(exception2);
			}
		}, null);
		return taskCompletionSource.Task;
	}

	internal static Task<T> AwaitAsyncOperation<T>(AsyncOperation operation, Func<T> getResult, CancellationToken cancellationToken)
	{
		if (operation == null)
		{
			throw new ArgumentNullException("operation");
		}
		if (getResult == null)
		{
			throw new ArgumentNullException("getResult");
		}
		if (operation.isDone)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(getResult());
		}
		TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		operation.completed += Complete;
		if (cancellationToken.CanBeCanceled)
		{
			cancellationToken.Register(delegate
			{
				operation.completed -= Complete;
				taskCompletionSource.TrySetCanceled(cancellationToken);
			});
		}
		return taskCompletionSource.Task;
		void Complete(AsyncOperation _)
		{
			operation.completed -= Complete;
			if (cancellationToken.IsCancellationRequested)
			{
				taskCompletionSource.TrySetCanceled(cancellationToken);
				return;
			}
			try
			{
				taskCompletionSource.TrySetResult(getResult());
			}
			catch (Exception exception)
			{
				taskCompletionSource.TrySetException(exception);
			}
		}
	}
}
