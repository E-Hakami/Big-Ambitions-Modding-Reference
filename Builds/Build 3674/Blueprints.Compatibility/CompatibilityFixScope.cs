using System;

namespace Blueprints.Compatibility;

[Flags]
public enum CompatibilityFixScope
{
	None = 0,
	Metadata = 1,
	Layout = 2,
	Both = Metadata | Layout
}
