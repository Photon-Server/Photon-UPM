namespace Fusion.Addons.Inspector.Editor
{
	using System.Reflection;

	internal sealed class RPCInfo
	{
		public readonly MethodInfo    Method;
		public readonly string        MethodName;
		public readonly string        MethodHierarchy;
		public readonly string        MethodDeclaration;
		public readonly ComponentInfo Component;

		public readonly RpcSources    Sources;
		public readonly RpcTargets    Targets;
		public readonly bool          IsReliable;
		public readonly bool          IsTickAligned;
		public readonly bool          HasRpcTarget;
		public readonly bool          InvokeLocal;

		public readonly IntLabel      Sent;
		public readonly IntLabel      Received;

		private int  _totalSent;
		private int  _totalReceived;
		private int  _baselineSent;
		private int  _baselineReceived;
		private bool _pendingBaseline;

		public RPCInfo(ComponentInfo component, MethodInfo method)
		{
			Method            = method;
			MethodName        = method.Name;
			MethodHierarchy   = $"{component.TypeName}.{MethodName}";
			MethodDeclaration = ReflectionUtility.GetMethodDeclaration(method);
			Component         = component;

			RpcAttribute rpc = method.GetCustomAttribute<RpcAttribute>();

			Sources       = (RpcSources)rpc.Sources;
			Targets       = (RpcTargets)rpc.Targets;
			IsReliable    = rpc.Channel == RpcChannel.Reliable;
			IsTickAligned = rpc.TickAligned;
			InvokeLocal   = rpc.InvokeLocal;

			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (parameter.IsDefined(typeof(RpcTargetAttribute), false) == true)
				{
					HasRpcTarget = true;
					break;
				}
			}

			Sent     = new IntLabel(0);
			Received = new IntLabel(0);
		}

		public void ProcessEvent(RPCEvent rpcEvent)
		{
			if (rpcEvent.Type == NetworkRunnerDebugRpcEventType.LocalCall)
			{
				++_totalSent;
			}
			else if (rpcEvent.Type == NetworkRunnerDebugRpcEventType.RemoteCall)
			{
				++_totalReceived;
			}
		}

		public void ClearCounts()
		{
			_totalSent     = 0;
			_totalReceived = 0;

			_baselineSent     = 0;
			_baselineReceived = 0;
		}

		public void UpdateCounts()
		{
			if (_pendingBaseline == true)
			{
				_pendingBaseline = false;

				_baselineSent     = _totalSent;
				_baselineReceived = _totalReceived;
			}

			Sent.Value     = _totalSent     - _baselineSent;
			Received.Value = _totalReceived - _baselineReceived;
		}

		public void ResetSession()
		{
			_pendingBaseline = true;

			Sent.Value     = 0;
			Received.Value = 0;
		}
	}
}
