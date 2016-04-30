using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocietyModel.Common
{
	public static class ListExtensions
	{
		/// <summary>
		/// Работает с классами.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="items"></param>
		/// <param name="rnd"></param>
		/// <param name="except"></param>
		/// <returns></returns>
		public static T GetRandom<T>(this IList<T> items, Random rnd, T except)
		{
			int targetIndex = rnd.Next(items.Count - 1);
			T item = items[targetIndex];

			if (object.ReferenceEquals(item, except))
				item = items[targetIndex++];

			return item;
		}

		public static T FirstMax<T>(this IList<T> list,
			Func<T, double> selector, int exceptIndex = -1)
		{
			return list.FirstMaxAndIndex(selector).Item1;
		}

		public static T FirstMin<T>(this IList<T> list,
			Func<T, double> selector, int exceptIndex = -1)
		{
			return list.FirstMinAndIndex(selector).Item1;
		}

		public static Tuple<double, int> FirstMaxAndIndex(this IList<double> list,
			int exceptIndex = -1)
		{
			return list.FirstMaxAndIndex(i => i, exceptIndex);
		}

		public static Tuple<double, int> FirstMinAndIndex(this IList<double> list,
			int exceptIndex = -1)
		{
			return list.FirstMinAndIndex(i => i, exceptIndex);
		}

		public static Tuple<T, int> FirstMaxAndIndex<T>(this IList<T> list,
			Func<T, double> selector, int exceptIndex = -1)
		{
			int indexAtMax = -1;
			T objectAtMax = default(T);
			double maxValue = double.MinValue;

			for (int i = 0; i < list.Count; i++)
			{
				T currentObject = list[i];
				double value = selector(currentObject);

				if ((i != exceptIndex) && (i == 0 || value > maxValue))
				{
					indexAtMax = i;
					objectAtMax = currentObject;
					maxValue = value;
				}
			}

			return new Tuple<T, int>(objectAtMax, indexAtMax);
		}

		public static Tuple<T, int> FirstMinAndIndex<T>(this IList<T> list,
			Func<T, double> selector, int exceptIndex = -1)
		{
			int indexAtMin = -1;
			T objectAtMin = default(T);
			double minValue = double.MaxValue;

			for (int i = 0; i < list.Count; i++)
			{
				T currentObject = list[i];
				double value = selector(currentObject);

				if ((i != exceptIndex) && (i == 0 || value < minValue))
				{
					indexAtMin = i;
					objectAtMin = currentObject;
					minValue = value;
				}
			}

			return new Tuple<T, int>(objectAtMin, indexAtMin);
		}
	}
}
