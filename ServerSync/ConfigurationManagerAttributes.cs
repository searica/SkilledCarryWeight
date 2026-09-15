using System;

namespace ServerSync;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
internal class ConfigurationManagerAttributes : Attribute
{
	public bool? ShowRangeButtons;

	public string? Category;

	public string? Name;

	public string? Description;

	public int? Order;

	public bool? ReadOnly;

	public bool? Browsable;

	public string? CustomDrawer;

	public bool? IsAdminOnly;
}
