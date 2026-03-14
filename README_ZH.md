基于[05126619z/ScavTemplate](https://github.com/05126619z/ScavTemplate).

[English Guide](README.md)
 
# BlackMossTemplate

一个用于开发 `Casualties Unknown` 的模组模板。

# 如何使用？
__这个指南适用于 JetBrains Rider，我不喜欢 Visual Studio，所以不会写 Visual Studio 的指南。__

*也许 Visual Studio 也能做到，但是我不会。*

1. 克隆[这个模板](https://github.com/new?template_name=BlackMossTemplate)。
2. 获取游戏的 dll 文件：
   1. 右键单击 `依赖项`。
   2. 选择 `引用...`，然后是 `添加自...`.
   3. 去到你游戏的目录(就像 `E:/CasualtiesUnknownDemo/CasualtiesUnknown_Data/Managed` 这样)。
   4. 选择所有 `.dll` 文件
3. 把 `BlackMossTemplate` 重命名为你的模组名称。
4. 把下列文件中的 `BlackMossTemplate` 换成你的模组名称：
   1. `Plugin.cs`
   2. `BlackMossTemplate.csproj`
5. 构建项目测试能不能运行，如果不能，重新尝试一遍 2、3、4、5 的步骤。
6. 成功运行之后，把 [Pulgin.cs](Plugin.cs) 中的以下内容替换成对应的内容：
   1. `namespace BlackMossTemplate` 改成你的命名空间，和你的模组名称一样就行（不能有空格）。
   2. `blackmoss.template` 换成你的GUID，格式是`你的名字.模组名称`。___兼容大小写和下划线，但是更推荐全小写无下划线。___
   3. `Black_Moss Template` 换成你的模组名称。
   4. `0.0.0` 版本你自行填写，`114514.1919.810` 都是可以的。
   5. `_harmony` 常量的内容和你的 GUID 是一样的。
   6. `Logger.LogInfo("Here's Black Moss!");` 这个你随便写 ~~（被骂在日志里拉屎不关我事）~~。

# 关于 StartGame.ps1
你应该看见了一个 [StartGame.ps1](StartGame.ps1) 文件，它可以将编译好的dll文件放到游戏目录下的BepInEx插件目录中。
可以直接在cmd/PowerShell中运行它，依次加上游戏目录和模组命名空间即可再启动它即可。

当然也可以使用Rider/Visual Studio来运行，这样更方便于开发。

## Rider:

直接右键 [StartGame.ps1](StartGame.ps1) 选择`运行 'StartGame.ps1'`，然后运行窗口的窗口会显示一堆报错，这个时候再点击Rider编辑器右上角构建按钮旁边刚刚出现的 `StartGame.ps1` 按钮，选择`编辑配置...`，`Script arguments:` 填写你的游戏目录和模组命名空间，`Command parameters` 填写 `-ExecutionPolicy Bypass`，按下面的`执行前`旁边的加号，选`构建解决方案`，确定之后就可以了。

之后每次按下构建旁边的绿三角按钮，就会自动把模组的dll文件复制到游戏目录下的BepInEx插件目录中并自动运行游戏，并且你还能在运行窗口中查看BepInEx的运行日志。

## Visual Studio:
我不知道啊，右键之后没运行的按钮，我又不用VS，懒得研究了你自己想想办法。:P