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

		public bool AutoIncreasePositiveness { get; set; }

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
		public double TempInfluenceStrategyCoef { get; set; }

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
							double eff = GetInfluenceEffectiveness(t, shouldActPositive);
							//double amount = Wealth * 0.15 * eff * rndCoef;
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
							t.TempInfluenceStrategyCoef = 1;

							ps.Add(t);
						}
					}
				}

				double max = ps.Max(p => 
					p.TempInfluenceAbsAmount * p.TempInfluenceStrategyCoef);

				/// ищем того, на кого можем повлиять больше всего 
				var target = ps.FirstOrDefault(p => 
					p.TempInfluenceAbsAmount * p.TempInfluenceStrategyCoef == max);

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

			if (AutoIncreasePositiveness && Positiveness.HasValue)
			{
				NewPositiveness = Positiveness + 0.02;

				if (NewPositiveness > 1)
					NewPositiveness = 1;

				return;
			}

			List<Personage> personages = new List<Personage>();

			for (int x = XIndex - 1; x <= XIndex + 1; x++)
			{
				for (int y = YIndex - 1; y <= YIndex + 1; y++)
				{
					if (env.IsCoordOk(x, y))
					{
						var source = env.PersonageMatrix[x, y];

						if (source.Positiveness.HasValue)
							personages.Add(source);
					}
				}
			}

			if (personages.Any())
			{
				double sum = 0;
				double effSum = 0;

				foreach (var p in personages)
				{
					var eff = GetPositivenessChangeEffectiveness(p);
					sum += p.Positiveness.Value * eff;
					effSum += eff;
				}

				/// учитываем собственную стратегию
				if (Positiveness.HasValue)
				{
					/// скорость смещения делаем не слишком быстрой,
					/// приравнивая свою прошлую стратегию к стратегиям k
					/// новых эффективно влияющих соседей
					double k = 6;
					sum += k * Positiveness.Value;
					effSum += k;
				}

				double avg = sum / effSum;

				NewPositiveness = avg;
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

		double GetInfluenceEffectiveness(Personage target, bool isHelp)
		{
			double eff1;

			if (Wealth < target.Wealth / 2.0)
				eff1 = 0;
			if (Wealth < target.Wealth)
				eff1 = 0.3;
			else if (Wealth < 2 * target.Wealth)
				eff1 = 0.7;
			else
				eff1 = 1;

			double eff2;

			if (!target.Positiveness.HasValue)
				/// типа мало что умеет пока что
				eff2 = 0.5;
			else if (isHelp)
			{
				/// если помогаем более позитивному, то он точно знает как
				/// распорядиться нашей помощью, эффективность 100%
				if (target.Positiveness >= Positiveness)
					eff2 = 1;
				//// если помогаем тому, кто менее чем в 2 раза отличается
				/// от нашей позитивности в сторону негативности,
				/// то эффективность считаем 70%
				/// (помощь - это не только еда и крыша над головой, но и возможности
				/// для самореализации, которые не каждый умеет использовать с полной отдачей)
				else if (target.Positiveness >= Positiveness / 2.0)
					eff2 = 0.7;
				else
					eff2 = 0.3;
			}
			else
			{
				/// если отнимаем у более позитивного, то делается это не сложно
				/// с физической точки зрения
				if (target.Positiveness >= 2 * Positiveness)
					eff2 = 1;
				if (target.Positiveness >= Positiveness)
					eff2 = 0.7;
				/// отъем у того, кто более негативен, сложнее
				else if (target.Positiveness >= Positiveness / 2.0)
					eff2 = 0.5;
				else
					eff2 = 0.2;

				/// ничего не будет делать
				eff1 = eff2 = 0;
			}

			return eff1 * eff2;
		}

		double GetPositivenessChangeEffectiveness(Personage source)
		{
			/// у более богатых стратегия перенимается быстрее, потому
			/// что подсознательно есть стремление жить так же хорошо
			if (source.Wealth > Wealth)
				return 1;
			else if (source.Wealth >= Wealth / 2.0)
				return 0.5;
			else
				return 0.2;
		}
	}
}
