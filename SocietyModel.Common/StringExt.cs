using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace SocietyModel.Common
{
	public static class StringExt
	{
		public static int ParseInt(this string source, int defaultValue = 0)
		{
			int res;

			if (int.TryParse(source, out res))
				return res;
			else
				return defaultValue;
		}

		public static double ParseDouble(this string source, double defaultValue = 0)
		{
			double res;

			if (double.TryParse(source, System.Globalization.NumberStyles.Number,
				CultureInfo.InvariantCulture, out res))
				return res;
			else
				return defaultValue;
		}

		public static string F(this string format, params object[] args)
		{
			return string.Format(format, args);
		}
	}
}
