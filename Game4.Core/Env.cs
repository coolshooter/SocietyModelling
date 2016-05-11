using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Game4.Core
{
	/// <summary>
	/// Среда обитания.
	/// </summary>
	public class Env
	{
		// размеры поля
		public int Width = 12;
		public int Height = 9;

		/// <summary>
		/// Поле персонажей.
		/// Могут взаимодействовать только находящиеся рядом.
		/// </summary>
		public Personage[,] PersonageMatrix;
		public List<Personage> AllPersonages;

		public Env(double? positiveness1 = 1, bool allowChange1 = false, 
			double? positiveness2 = null, bool allowChange2 = true,
			bool increase2 = false,
			double? positiveness3 = null)
		{
			if (positiveness1 < 0 || positiveness1 > 1 ||
				positiveness2 < 0 || positiveness2 > 1 ||
				positiveness3 < 0 || positiveness3 > 1)
				throw new ArgumentException("Позитивность должна быть в интервале 0..1");

			PersonageMatrix = new Personage[Width, Height];
			AllPersonages = new List<Personage>();

			for (int x = 0; x < PersonageMatrix.GetLength(0); x++)
			{
				for (int y = 0; y < PersonageMatrix.GetLength(1); y++)
				{
					Personage p = new Personage(x, y, null);
					p.Positiveness = positiveness3;
					PersonageMatrix[x, y] = p;
					AllPersonages.Add(p);
                }
			}

			int p1X = 3;
			int p2X = Width - 4;

			if (p1X >= p2X)
				throw new ArgumentException("Слишком малый размер поля по X", nameof(Width));

			/// делаем персонажи в центре поля ключевыми
			var p1 = PersonageMatrix[p1X, Height / 2];
			p1.Positiveness = positiveness1;
			p1.IsKey = true;
			p1.AllowChangePositiveness = allowChange1;

			var p2 = PersonageMatrix[p2X, Height / 2];
			p2.Positiveness = positiveness2;
			p2.IsKey = true;
			/// тут лучше даже true поставить в будущем, но интересны оба значения
			p2.AllowChangePositiveness = allowChange2;
			p2.AutoIncreasePositiveness = increase2;
			if (p2.AutoIncreasePositiveness)
				p2.AllowChangePositiveness = true;
        }

		public async Task Run(bool allowMigration, Action<Personage> updateUI = null,
			Action<List<Personage>, int> updateUISummary = null)
		{
			/// цикл по итерациям
			for (int i = 0; i < 150; i++)
			{
				foreach (var p in AllPersonages)
				{
					p.NewWealth = p.Wealth;
					p.NewPositiveness = p.Positiveness;
				}

				foreach (var p in AllPersonages)
				{
					p.Act(this);
				}

				foreach (var p in AllPersonages)
				{
					p.CalcPositiveness(this);
				}

				foreach (var p in AllPersonages)
				{
					p.Wealth = p.NewWealth;

					if (p.NewPositiveness.HasValue)
						p.Positiveness = p.NewPositiveness.Value;

					if (updateUI != null)
						updateUI(p);
				}

				if (allowMigration)
				{
					foreach (var p in AllPersonages)
					{
						if (!p.IsKey && Svc.Rnd.NextDouble() < 0.2)
							p.Migrate(this);
					}
				}

				if (updateUISummary != null)
					updateUISummary(AllPersonages, i);

				if (updateUI != null)
					await Task.Delay(30);
			}
		}

		public bool IsCoordOk(int x, int y)
		{
			return x >= 0 && y >= 0 && x < Width && y < Height;
        }
	}
}
