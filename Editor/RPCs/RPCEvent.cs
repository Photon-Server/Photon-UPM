namespace Fusion.Addons.Inspector.Editor
{
	using System.Reflection;

	internal struct RPCEvent
	{
		public NetworkRunnerDebugRpcEventType Type;
		public int                            Frame;
		public int                            Tick;
		public MethodBase                     Method;
		public NetworkBehaviour               Behaviour;
		public NetworkObject                  Object;
		public string                         ObjectName;
		public PlayerRef                      SourcePlayer;
		public PlayerRef                      TargetPlayer;
		public PlayerRef                      StateAuthority;
		public PlayerRef                      InputAuthority;
		public PlayerRef                      LocalPlayer;
		public RpcInvokeInfo?                 InvokeInfo;
		public bool                           HasStateAuthority;
		public bool                           HasInputAuthority;
	}
}
