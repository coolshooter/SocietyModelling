using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game4.Core
{
	/// <summary>
	/// Персонаж моделируемого общества.
	/// </summary>
	public class Personage
	{
		const double MIN_WEALTH = 0.01;

		/// <summary>
		/// Ключевой ли персонаж.
		/// </summary>
		public bool IsKey { get; set; }

		/// <summary>
		/// Позитивность персонажа - число от 0 до 1 включительно.
		/// 1 = действует в интересах общества 
		/// (как результат, - себя самого вместе с обществом в долгосрочной перспективе),
		/// 0 = в интересах себя самого в краткосрочной перспективе.
		/// Null = стратегии нет, берется на первой итерации как среднее поведение 
		/// окружающих, у которых она известна (если таких нет, - остается null, что
		/// ведет к пропуску хода).
		/// </summary>
		public double? Positiveness { get; set; }

		/// <summary>
		/// Временное значение для расчета нового значения позитивности.
		/// </summary>
		public double? NewPositiveness { get; set; }

		/// <summary>
		/// Может ли в процессе игры меняться позитивность персонажа.
		/// Как правило - да, кроме ключевых персонажей.
		/// </summary>
		public bool AllowChangePositiveness { get; set; }

		/// <summary>
		/// Уровень жизни, объем имеющихся ресурсов.
		/// Влияет на объем помощи окружающим на каждом ходу.
		/// </summary>
		public double Wealth { get; set; }

		/// <summary>
		/// Временное значение для расчета нового значения благосостояния.
		/// </summary>
		public double NewWealth { get; set; }

		public int XIndex { get; set; }
		public int YIndex { get; set; }

		public double TempInfluenceAbsAmount { get; set; }

		public Personage(int xIndex, int yIndex, double? positiveness = null)
		{
			XIndex = xIndex;
			YIndex = yIndex;

			Wealth = 1;
			Positiveness = positiveness;
			AllowChangePositiveness = true;
		}

		public void Act(Env env)
		{
			if (Positiveness.HasValue)
			{
				bool shouldActPositive = Positiveness > 0 &&
					Svc.Rnd.NextDouble() <= Positiveness;

				List<Personage> ps = new List<Personage>();

				for (int x = XIndex - 1; x <= XIndex + 1; x++)
				{
					for (int y = YIndex - 1; y <= YIndex + 1; y++)
					{
						if (env.IsCoordOk(x, y))
						{
							var t = env.PersonageMatrix[x, y];
							double rndCoef = Svc.Rnd.NextDouble();
							double eff = GetInfluenceEffectiveness(t);
							double amount = Wealth * 0.2 * eff * rndCoef;

							/// уменьшаем объем влияния в соответствии с неуверенностью
							/// персонажа в принадлежности к стратегиям
							if (shouldActPositive)
								amount = Positiveness.Value * amount;
							else
								amount = (1 - Positiveness.Value) * amount;

							/// корректируем объем отъема, чтобы не выйти за рамки имеющегося 
							/// объема ресурсов таргета
							if (!shouldActPositive)
								amount = Math.Min(t.NewWealth - MIN_WEALTH, amount);

							t.TempInfluenceAbsAmount = amount;

							ps.Add(t);
						}
					}
				}

				double maxAmount = ps.Max(p => p.TempInfluenceAbsAmount);

				/// ищем того, на кого можем повлиять больше всего 
				var target = ps.FirstOrDefault(p => p.TempInfluenceAbsAmount == maxAmount);

				if (shouldActPositive)
					target.NewWealth += target.TempInfluenceAbsAmount;
				else
				{
					target.NewWealth -= target.TempInfluenceAbsAmount;
					NewWealth += target.TempInfluenceAbsAmount;
                }
			}
		}

		public void CalcPositiveness(Env env)
		{
			if (!AllowChangePositiveness)
				return;

			List<double> values = new List<double>();

			for (int x = XIndex - 1; x <= XIndex + 1; x++)
			{
				for (int y = YIndex - 1; y <= YIndex + 1; y++)
				{
					if (env.IsCoordOk(x, y))
					{
						var source = env.PersonageMatrix[x, y];

						if (source.Positiveness.HasValue)
							values.Add(source.Positiveness.Value);
					}
				}
			}

			if (values.Any())
			{
				double avg = values.Average();

				if (!NewPositiveness.HasValue)
					NewPositiveness = avg;
				else
					/// смещаемся на половину расстояния между своим предыдущим
					/// состоянием и средним состоянием окружающих (у которых стратегия задана)
					NewPositiveness = (NewPositiveness.Value + avg) / 2.0;
			}
		}

		public void Migrate(Env env)
		{
			/// не можем меняться местами с ключевым персонажем

			int newX = XIndex + Svc.Rnd.Next(3) - 1;
			int newY = YIndex + Svc.Rnd.Next(3) - 1;

			if (!IsKey && env.IsCoordOk(newX, newY) && !env.PersonageMatrix[newX, newY].IsKey)
			{
				var p = env.PersonageMatrix[newX, newY];
				env.PersonageMatrix[XIndex, YIndex] = p;
				env.PersonageMatrix[newX, newY] = this;

				p.XIndex = XIndex;
				p.YIndex = YIndex;

				XIndex = newX;
				YIndex = newY;
			}
		}

		double GetInfluenceEffectiveness(Personage target)
		{
			if (Wealth < target.Wealth)
				return 0;
			else if (Wealth < 2 * target.Wealth)
				return 0.5;
			else
				return 1;
		}
	}
}
