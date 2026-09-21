namespace Fusion.Addons.Inspector.Editor
{
	using System;

	[Flags]
	internal enum EComponentField : long
	{
		None                           = 0L,
		Type                           = 1L <<  0,

		HasSpawned                     = 1L <<  1,
		HasDespawned                   = 1L <<  2,
		HasFixedUpdateNetwork          = 1L <<  3,
		HasRender                      = 1L <<  4,

		HasAfterSpawned                = 1L <<  5,
		HasAfterHostMigration          = 1L <<  6,
		HasStateAuthorityChanged       = 1L <<  7,
		HasInputAuthorityGained        = 1L <<  8,
		HasInputAuthorityLost          = 1L <<  9,
		HasSimulationEnter             = 1L << 10,
		HasSimulationExit              = 1L << 11,
		HasInterestEnter               = 1L << 12,
		HasInterestExit                = 1L << 13,

		HasBeforeUpdate                = 1L << 14,
		HasBeforeCopyPreviousState     = 1L << 15,
		HasBeforeClientPredictionReset = 1L << 16,
		HasAfterClientPredictionReset  = 1L << 17,
		HasBeforeSimulation            = 1L << 18,
		HasBeforeAllTicks              = 1L << 19,
		HasBeforeTick                  = 1L << 20,
		HasAfterTick                   = 1L << 21,
		HasAfterAllTicks               = 1L << 22,
		HasAfterRender                 = 1L << 23,
		HasAfterUpdate                 = 1L << 24,

		SceneObjects                   = 1L << 25,
		Prefabs                        = 1L << 26,
		Properties                     = 1L << 27,
		RPCs                           = 1L << 28,
		StateSize                      = 1L << 29,
		ExecutionOrder                 = 1L << 30,
		TotalStateChanges              = 1L << 31,
		AverageStateChanges            = 1L << 32,

		VirtualMethods                 = HasSpawned | HasDespawned | HasFixedUpdateNetwork | HasRender,
		Interfaces1                    = HasAfterSpawned | HasAfterHostMigration | HasStateAuthorityChanged | HasInputAuthorityGained | HasInputAuthorityLost | HasSimulationEnter | HasSimulationExit | HasInterestEnter | HasInterestExit,
		Interfaces2                    = HasBeforeUpdate | HasBeforeCopyPreviousState | HasBeforeClientPredictionReset | HasAfterClientPredictionReset | HasBeforeSimulation | HasBeforeAllTicks | HasBeforeTick | HasAfterTick | HasAfterAllTicks | HasAfterRender | HasAfterUpdate,
		Other                          = SceneObjects | Prefabs | Properties | RPCs | StateSize | ExecutionOrder | TotalStateChanges | AverageStateChanges,
	}

	internal static class ComponentFieldExtensions
	{
		public static bool Has(this EComponentField fields, EComponentField field)
		{
			return (fields & field) == field;
		}
	}
}
