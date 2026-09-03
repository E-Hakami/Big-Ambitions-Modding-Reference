using System.Collections.Generic;

namespace Entities;

public class HelpCategoryEntry
{
	public class HelpPageEntry
	{
		public string Name;

		public string PageContent;
	}

	public string Name;

	public List<HelpPageEntry> Pages = new List<HelpPageEntry>();
}
