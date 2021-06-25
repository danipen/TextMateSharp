using TextMateSharp.Internal.Oniguruma;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Rules
{
	public abstract class Rule
	{

		public int? id;

		private bool nameIsCapturing;
		private string name;

		private bool contentNameIsCapturing;
		private string contentName;

		public Rule(int? id, string name, string contentName)
		{
			this.id = id;
			this.name = name;
			this.nameIsCapturing = RegexSource.HasCaptures(this.name);
			this.contentName = contentName;
			this.contentNameIsCapturing = RegexSource.HasCaptures(this.contentName);
		}

		public string GetName(string lineText, IOnigCaptureIndex[] captureIndices)
		{
			if (!this.nameIsCapturing)
			{
				return this.name;
			}

			return RegexSource.ReplaceCaptures(this.name, lineText, captureIndices);
		}

		public string GetContentName(string lineText, IOnigCaptureIndex[] captureIndices)
		{
			if (!this.contentNameIsCapturing)
			{
				return this.contentName;
			}
			return RegexSource.ReplaceCaptures(this.contentName, lineText, captureIndices);
		}

		public abstract void CollectPatternsRecursive(IRuleRegistry grammar, RegExpSourceList sourceList, bool isFirst);

		public abstract ICompiledRule Compile(IRuleRegistry grammar, string endRegexSource, bool allowA, bool allowG);
	}
}