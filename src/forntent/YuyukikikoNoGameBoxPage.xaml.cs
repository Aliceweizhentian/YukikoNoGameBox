using DGLabGameController;
using DGLabGameController.Core.Config;
using DGLabGameController.Core.Debug;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using YuyukikikoNoGameBox.ViewModels;
using YuyukikikoNoGameBox.utils;

namespace YuyukikikoNoGameBox
{
	public partial class YuyukikikoNoGameBoxPage : UserControl
	{

		private readonly YuyukikikoNoGameBoxPageViewModel _viewModel;


		public YuyukikikoNoGameBoxPage(string moduleId)
		{
			//配置DLL路径
			string moduleFolderPath = Path.Combine(AppConfig.ModulesPath, moduleId);
			bool su = HookDllWrapper.SetupDllDirectory(moduleFolderPath);
			InitializeComponent();
			//配置ViewModel
			_viewModel = new YuyukikikoNoGameBoxPageViewModel(moduleFolderPath);
			DataContext = _viewModel;
		}

		public void Back_Click(object sender, RoutedEventArgs e)
		{
			if (Application.Current.MainWindow is MainWindow mw) mw.CloseActiveModule();
			else DebugHub.Warning("返回失败", "主人...我不知道该回哪里去呢？");
		}

	}
}