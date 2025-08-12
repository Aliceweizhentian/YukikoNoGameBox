using DGLabGameController.Core.Module;
using System.Windows.Controls;

namespace YuyukikikoNoGameBox
{
	public class Main : ModuleBase
	{
		public override string ModuleId => "YuyukikikoNoGameBox"; // 模块唯一ID, 与文件夹及DLL名称一致
		public override string Name => "Yuyukikiko的游戏盒"; // 模块名称
		public override string Description => "实时检测一些游戏的玩家血条，玩家掉血触发惩罚"; // 模块描述
		public override string Version => "V1.0.0"; // 模块版本
		public override string Author => "Aliceweizhentian"; // 模块作者
		public override int CompatibleApiVersion => 10087; // 兼容的API版本, 通常不会更改

		protected override UserControl CreatePage()
		{
			return new YuyukikikoNoGameBoxPage(ModuleId);
		}
	}
}
