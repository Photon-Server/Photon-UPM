namespace Fusion.Addons.Inspector.Editor
{
	using System;

	[Flags]
	internal enum ERPCField
	{
		None          = 0,
		Name          = 1 <<  0,

		Sources       = 1 <<  1,
		Targets       = 1 <<  2,
		IsReliable    = 1 <<  3,
		IsTickAligned = 1 <<  4,
		HasRpcTarget  = 1 <<  5,
		InvokeLocal   = 1 <<  6,

		Sent          = 1 <<  7,
		Received      = 1 <<  8,

		Configuration = Sources | Targets | IsReliable | IsTickAligned | HasRpcTarget | InvokeLocal,
		Runtime       = Sent | Received,
	}

	internal static class RPCFieldExtensions
	{
		public static bool Has(this ERPCField fields, ERPCField field)
		{
			return (fields & field) == field;
		}
	}
}
