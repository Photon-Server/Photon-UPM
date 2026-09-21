namespace Fusion.Addons.Inspector.Editor
{
	public sealed class PlayerRefLabel
	{
		public PlayerRef Value;

		private PlayerRef _labelValue;
		private string    _labelString = "None";

		public string GetLabel()
		{
			if (_labelValue != Value)
			{
				_labelValue = Value;

				if (_labelValue.IsRealPlayer == true)
				{
					_labelString = $"Player {_labelValue.PlayerId}";
				}
#if FUSION_2_1_OR_NEWER
				else if (_labelValue.IsInternalMasterClientIdentifier == true)
#else
				else if (_labelValue.IsMasterClient == true)
#endif
				{
					_labelString = $"Master";
				}
				else if (_labelValue.IsNone == true)
				{
					_labelString = $"None";
				}
				else
				{
					_labelString = $"Invalid";
				}
			}

			return _labelString;
		}
	}
}
