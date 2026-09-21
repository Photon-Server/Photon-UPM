namespace Fusion.Addons.Inspector.Editor
{
	using System;

	[Flags]
	internal enum ERPCEventField
	{
		None           = 0,
		RPC            = 1 << 0,

		Type           = 1 << 1,
		Object         = 1 << 2,
		Tick           = 1 << 3,
		Source         = 1 << 4,
		Target         = 1 << 5,
		StateAuthority = 1 << 6,
		InputAuthority = 1 << 7,
		Frame          = 1 << 8,
		Result         = 1 << 9,

		General        = Object | Frame | Tick | Source | Target | StateAuthority | InputAuthority | Result,
	}

	internal static class RPCEventFieldExtensions
	{
		public static bool Has(this ERPCEventField fields, ERPCEventField field)
		{
			return (fields & field) == field;
		}
	}
}
