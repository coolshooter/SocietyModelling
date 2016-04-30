using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocietyModel.Common
{
	public static class RndExtensions
	{
		public static bool NextBool(this Random rnd)
		{
			return rnd.Next(2) == 1;
		}

		public static int RoundUsingPossibility(this Random rnd, double value)
		{
			int floor = (int)Math.Floor(value);
			double fractionalPart = value - floor;

			/// небольшая оптимизация
			if (fractionalPart > 0.05 && fractionalPart < 0.95)
			{
				if (rnd.NextDouble() <= fractionalPart)
					return floor + 1;
				else
					return floor;
			}
			else
				return (int)Math.Round(value);
		}
	}
}
