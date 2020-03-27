namespace NBitcoin.RPC
{
	public class VerboseOutputInfo
	{
		public VerboseOutputInfo(Money value, Script scriptPubKey)
		{
			Value = value;
			ScriptPubKey = scriptPubKey;
		}

		public Money Value { get; }

		public Script ScriptPubKey { get; }
	}
}
