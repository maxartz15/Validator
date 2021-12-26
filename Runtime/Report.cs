using System.Collections.Generic;
using UnityEngine;

namespace Validator
{
	public enum WarningType
	{
		Info,
		Warning,
		Error,
	}

	public class Report
	{
		public struct ReportMessage
		{
			public Object Target => target;
			public WarningType WarningType => warningType;
			public string Category => category;
			public string Message => message;
			public string Solution => solution;

			private readonly Object target;
			private readonly WarningType warningType;
			private readonly string category;
			private readonly string message;
			private readonly string solution;

			public ReportMessage(Object target, WarningType warningLevel, string category, string message, string solution)
			{
				this.target = target;
				this.warningType = warningLevel;
				this.category = category;
				this.message = message;
				this.solution = solution;
			}
		}

		public string Name => name;

		public IList<ReportMessage> Reports => reports.AsReadOnly();
		private readonly string name;
		private readonly List<ReportMessage> reports = new List<ReportMessage>();

		public Report(string name)
		{
			this.name = name;
		}

		public void Log(ReportMessage report)
		{
			reports.Add(report);
		}

		public void Log(Object target, WarningType warningType, string category, string message, string solution)
		{
			Log(new ReportMessage(target, warningType, category, message, solution));
		}
	}
}