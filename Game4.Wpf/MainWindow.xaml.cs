using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Game4.Core;
using SocietyModel.Common;

namespace Game4.Wpf
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		Border[,] _elements;

		public MainWindow()
		{
			InitializeComponent();

			btnRun.Click += BtnRun_Click;
		}

		private async void BtnRun_Click(object sender, RoutedEventArgs e)
		{
			btnRun.IsEnabled = false;

			await BuildAndRun();

			btnRun.IsEnabled = true;
		}

		async Task BuildAndRun()
		{
			double positiveness1 = txtPositiveness1.Text.ParseDouble();
			double positiveness2 = txtPositiveness2.Text.ParseDouble();
			double positiveness3 = txtPositiveness3.Text.ParseDouble();

			if (positiveness1 < 0 || positiveness1 > 1 ||
				positiveness2 < 0 || positiveness2 > 1 ||
				positiveness3 < 0 || positiveness3 > 1)
			{
				MessageBox.Show("Стратегия должна быть в интервале 0..1");
			}
			else
			{
				Env env = new Env(positiveness1, chbStable1.IsChecked != true,
					positiveness2, chbStable2.IsChecked != true,
					chbIncrease2.IsChecked == true,
					positiveness3);

				AddUIElements(env, cnvMain);

				bool addInfoAboutLeftAndRight = !(positiveness1 == positiveness2 &&
					(chbStable1.IsChecked == true) == (chbStable2.IsChecked == true));

				await env.Run(false, p => SetColorAndInfo(p),
					(ps, i) => UpdateSummary(ps, i, env, addInfoAboutLeftAndRight));

				foreach (var p in env.AllPersonages)
					SetColorAndInfo(p);

				var keyPs = env.AllPersonages.Where(p => p.IsKey).ToList();
				var sorted = env.AllPersonages.OrderByDescending(p => p.Wealth).ToList();
			}
		}

		void AddUIElements(Env env, Canvas cnv)
		{
			_elements = new Border[env.Width, env.Height];
			cnv.Children.Clear();

			foreach (var p in env.AllPersonages)
			{
				Border border = new Border();
				border.BorderBrush = Brushes.Black;
				border.BorderThickness = p.IsKey ? new Thickness(2) : new Thickness(1);
				border.Width = 60;
				border.Height = 30;

				TextBlock lbl = new TextBlock();
				border.Child = lbl;

				cnv.Children.Add(border);

				Canvas.SetLeft(border, p.XIndex * border.Width);
				Canvas.SetTop(border, p.YIndex * border.Height);

				_elements[p.XIndex, p.YIndex] = border;

				SetColorAndInfo(p);
			}
		}

		void SetColorAndInfo(Personage p)
		{
			var border = _elements[p.XIndex, p.YIndex];
			SolidColorBrush backBrush;

			if (p.Positiveness.HasValue)
			{
				byte red = (byte)(255 * (1 - p.Positiveness));
				byte green = (byte)(255 * p.Positiveness);
				byte blue = (byte)(255 * (1 - p.Positiveness));

				backBrush = new SolidColorBrush(
					Color.FromRgb(red, green, blue));
			}
			else
				backBrush = Brushes.White;

			border.Background = backBrush;

			var lbl = ((TextBlock)border.Child);
			lbl.Text = RoundAndFormatValueForUI(p.Wealth);

			if (p.IsKey)
				lbl.TextDecorations = TextDecorations.Underline;
			else
				lbl.TextDecorations = null;
        }

		void UpdateSummary(List<Personage> allPersonages, int iteration, Env env,
			bool addInfoAboutLeftAndRight)
		{
			double avg = allPersonages.Average(p => p.Wealth);
			double min = allPersonages.Min(p => p.Wealth);
			double max = allPersonages.Max(p => p.Wealth);

			var leftPart = allPersonages
				.Where(p => p.XIndex <= Math.Round(env.Width / 2.0) - 1)
				.ToList();

			var rightPart = allPersonages
				.Where(p => p.XIndex >= Math.Round(env.Width / 2.0))
				.ToList();

			double avgLeft = leftPart.Average(p => p.Wealth);
			double avgRight = rightPart.Average(p => p.Wealth);

			lblInfo.Text = 
				"Шаг: " + (iteration + 1) +
				", средний уровень жизни: " + RoundAndFormatValueForUI(avg) +
				", минимальный: " + RoundAndFormatValueForUI(min) +
				", максимальный: " + RoundAndFormatValueForUI(max);

			if (addInfoAboutLeftAndRight)
			{
				lblInfo2.Text =
						string.Format("средний уровень в левой половине: {0}, в правой: {1}",
						RoundAndFormatValueForUI(avgLeft),
						RoundAndFormatValueForUI(avgRight));
			}
			else
				lblInfo2.Text = "";
        }

		string RoundAndFormatValueForUI(double value)
		{
			return Math.Round(value).ToString("N0");
		}
	}
}
