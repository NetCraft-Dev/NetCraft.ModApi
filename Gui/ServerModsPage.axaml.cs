using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NetCraft.Logging;
using NetCraft.ModLoader;

namespace NetCraft.ModApi.Gui;

//ServerModsPage 模组页
//左边列出扫描到的模组 右边是选中那个的展示信息与注入规则
//数据来自 ModHost 引导结束时登记的快照 模组集合之后不再变 页面只读
public sealed partial class ServerModsPage : UserControl
{
    //缺省图标 模组没带图标时用它 由构建期的 pwsh 脚本画出来
    private const string DefaultIconUri = "avares://NetCraft.ModApi/Gui/Assets/ModDefaultIcon.png";

    //_mods 列表当前展示的模组 下标与 ModList 的项一一对应
    private readonly List<ModInfo> _mods = new();
    //_icons 图标缓存 图标要从 dll 里读 不能每次刷新都重读一遍
    private readonly Dictionary<string, Bitmap?> _icons = new();
    private Bitmap? _defaultIcon;

    public ServerModsPage()
    {
        InitializeComponent();
        ModList.SelectionChanged += (_, _) => ShowDetail(SelectedMod());
        Reload();
    }

    //Refresh 主窗口轮询的入口
    //模组集合在引导之后固定 列表建一次就够 空着才说明还没建过
    public void Refresh()
    {
        if (_mods.Count == 0)
            Reload();
    }

    //Reload 重建左侧列表
    private void Reload()
    {
        _mods.Clear();
        _mods.AddRange(ModHost.AllMods);

        ModList.Items.Clear();
        foreach (var mod in _mods)
            ModList.Items.Add(BuildRow(mod));

        var folder = ModHost.ModsFolder;
        SummaryLabel.Text = _mods.Count == 0
            ? $"没有模组\n{folder}"
            : $"{_mods.Count} 个模组\n{folder}";

        if (_mods.Count > 0)
            ModList.SelectedIndex = 0;
        else
            ShowDetail(null);
    }

    //BuildRow 列表里的一行 小图标 + 名字 + 版本与状态
    private Control BuildRow(ModInfo mod)
    {
        var icon = new Image
        {
            Width = 28,
            Height = 28,
            Source = IconFor(mod.Name),
            VerticalAlignment = VerticalAlignment.Center,
        };

        var title = new TextBlock
        {
            Text = mod.DisplayName,
            Classes = { "listRow" },
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        var subtitle = new TextBlock
        {
            Text = Subtitle(mod),
            Classes = { "hintText" },
            Foreground = StatusBrush(mod.Status),
        };

        var text = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(title);
        text.Children.Add(subtitle);

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("28,10,*") };
        Grid.SetColumn(text, 2);
        row.Children.Add(icon);
        row.Children.Add(text);
        return row;
    }

    //ShowDetail 填右侧详情
    private void ShowDetail(ModInfo? mod)
    {
        DetailBody.Children.Clear();

        if (mod is null)
        {
            DetailIcon.Source = _defaultIcon;
            DetailName.Text = "没有模组";
            DetailSubtitle.Text = string.Empty;
            DetailPath.Text = string.Empty;
            return;
        }

        DetailIcon.Source = IconFor(mod.Name);
        DetailName.Text = mod.DisplayName;
        DetailSubtitle.Text = Subtitle(mod);
        DetailPath.Text = mod.AssemblyPath;

        AddSection("描述", Text(mod.Description.Length > 0 ? mod.Description : "这个模组没有写描述"));

        AddSection("信息", BuildInfo(mod));

        if (HasContact(mod.Contact))
            AddSection("链接", BuildLinks(mod.Contact));

        if (mod.Authors.Count > 0 || mod.Contributors.Count > 0 || mod.License.Length > 0)
            AddSection("署名", BuildCredits(mod));

        AddSection("依赖", BuildDependencies(mod));

        AddSection($"注入规则 {mod.Hooks.Count} 条", BuildHooks(mod));
    }

    //BuildInfo 标识 版本 运行端 状态 耗时
    private static Control BuildInfo(ModInfo mod)
    {
        var rows = new List<(string Key, string Value)>
        {
            ("标识", mod.Id),
            ("版本", mod.Version.Length > 0 ? mod.Version : "未标注"),
            ("运行端", EnvironmentText(mod.Environment)),
            ("状态", StatusText(mod.Status)),
            ("初始化", FormatLoadTime(mod.LoadMilliseconds)),
        };
        return KeyValuePanel(rows);
    }

    //FormatLoadTime 初始化耗时文案
    //未走到初始化写明文 负值表示这一栏不适用 返回空串让整行都不显示
    private static string FormatLoadTime(double? milliseconds) => milliseconds switch
    {
        null => "未走到初始化",
        < 0 => string.Empty,
        var ms => $"{ms:F1} ms",
    };

    //BuildCredits 作者 贡献者 许可证
    private static Control BuildCredits(ModInfo mod)
    {
        var rows = new List<(string Key, string Value)>();
        if (mod.Authors.Count > 0) rows.Add(("作者", string.Join("、", mod.Authors)));
        if (mod.Contributors.Count > 0) rows.Add(("贡献者", string.Join("、", mod.Contributors)));
        if (mod.License.Length > 0) rows.Add(("许可证", mod.License));
        return KeyValuePanel(rows);
    }

    //BuildLinks 三条对外链接 点一下丢给系统浏览器
    private Control BuildLinks(ModContact contact)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        AddLink("主页", contact.Homepage);
        AddLink("源码", contact.Sources);
        AddLink("问题", contact.Issues);
        return panel;

        void AddLink(string label, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            var button = new Button { Content = label, Padding = new Thickness(12, 4) };
            button.Click += (_, _) => OpenLink(url);
            ToolTip.SetTip(button, url);
            panel.Children.Add(button);
        }
    }

    //BuildDependencies 依赖与被依赖两侧 点名字能跳到那个模组
    private Control BuildDependencies(ModInfo mod)
    {
        var manager = ModHost.Manager;
        var dependencies = manager?.GetDependencies(mod.Name) ?? Array.Empty<string>();
        var dependents = manager?.GetModsDependingOn(mod.Name) ?? Array.Empty<string>();

        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(ChipRow("依赖", dependencies));
        panel.Children.Add(ChipRow("被依赖", dependents));
        return panel;
    }

    //ChipRow 一行可跳转的模组名 空的时候写"无"
    private Control ChipRow(string label, IReadOnlyList<string> names)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        row.Children.Add(new TextBlock
        {
            Text = label,
            Classes = { "metricLabel" },
            Width = 48,
            VerticalAlignment = VerticalAlignment.Center,
        });

        if (names.Count == 0)
        {
            row.Children.Add(new TextBlock
            {
                Text = "无",
                Classes = { "hintText" },
                VerticalAlignment = VerticalAlignment.Center,
            });
            return row;
        }

        foreach (var name in names)
        {
            var target = ModHost.Find(name);
            var button = new Button
            {
                Content = target?.DisplayName ?? name,
                Padding = new Thickness(8, 2),
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center,
            };
            button.Click += (_, _) => SelectMod(name);
            row.Children.Add(button);
        }
        return row;
    }

    //BuildHooks 注入规则逐条列出来 复刻清单里的写法
    private static Control BuildHooks(ModInfo mod)
    {
        if (mod.Hooks.Count == 0)
            return Text("没有注入规则");

        var panel = new StackPanel { Spacing = 3 };
        foreach (var hook in mod.Hooks)
        {
            var line = $"{hook.HookTypeName}  {hook.Target}.{hook.Method}";
            if (!string.IsNullOrEmpty(hook.Label))
                line += $"  [{hook.Label}]";
            if (hook.Environment != ModEnvironment.Both)
                line += $"  ({hook.EnvironmentValue})";

            panel.Children.Add(new TextBlock
            {
                Text = line,
                Classes = { "monoValue" },
                TextWrapping = TextWrapping.Wrap,
            });
        }
        return panel;
    }

    //AddSection 往详情里加一节 标题用卡片小标题的样式
    private void AddSection(string title, Control content)
    {
        var section = new StackPanel { Spacing = 5 };
        section.Children.Add(new TextBlock { Text = title, Classes = { "cardTitle" } });
        section.Children.Add(content);
        DetailBody.Children.Add(section);
    }

    //KeyValuePanel 左标签右值的一串行 值为空的跳过
    private static Control KeyValuePanel(IEnumerable<(string Key, string Value)> rows)
    {
        var panel = new StackPanel { Spacing = 3 };
        foreach (var (key, value) in rows)
        {
            if (value.Length == 0)
                continue;

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("72,*") };
            var text = new TextBlock
            {
                Text = value,
                Classes = { "monoValue" },
                TextWrapping = TextWrapping.Wrap,
            };
            Grid.SetColumn(text, 1);
            grid.Children.Add(new TextBlock { Text = key, Classes = { "metricLabel" } });
            grid.Children.Add(text);
            panel.Children.Add(grid);
        }
        return panel;
    }

    //SelectMod 跳到某个模组 依赖那一栏点名字走这里
    private void SelectMod(string name)
    {
        var index = _mods.FindIndex(m => m.Name == name);
        if (index >= 0)
            ModList.SelectedIndex = index;
    }

    private ModInfo? SelectedMod()
    {
        var index = ModList.SelectedIndex;
        return index >= 0 && index < _mods.Count ? _mods[index] : null;
    }

    //IconFor 取图标 清单没写或解不出来时退回缺省图
    private Bitmap? IconFor(string name)
    {
        if (_icons.TryGetValue(name, out var cached))
            return cached ?? DefaultIcon();

        Bitmap? bitmap = null;
        var bytes = ModHost.ReadIcon(name);
        if (bytes is not null)
        {
            try
            {
                bitmap = new Bitmap(new MemoryStream(bytes));
            }
            catch (Exception ex)
            {
                Log.Debug($"Mod {name} icon cannot be decoded {ex.Message}");
            }
        }

        _icons[name] = bitmap;
        return bitmap ?? DefaultIcon();
    }

    private Bitmap? DefaultIcon()
    {
        if (_defaultIcon is not null)
            return _defaultIcon;

        try
        {
            _defaultIcon = new Bitmap(AssetLoader.Open(new Uri(DefaultIconUri)));
        }
        catch (Exception ex)
        {
            Log.Debug($"Default mod icon failed to load {ex.Message}");
        }
        return _defaultIcon;
    }

    private void OpenLink(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return;

        var top = TopLevel.GetTopLevel(this);
        if (top is null)
            return;

        _ = top.Launcher.LaunchUriAsync(uri);
    }

    private static bool HasContact(ModContact contact)
        => contact.Homepage.Length > 0 || contact.Sources.Length > 0 || contact.Issues.Length > 0;

    private static TextBlock Text(string value) => new()
    {
        Text = value,
        Classes = { "listRow" },
        TextWrapping = TextWrapping.Wrap,
    };

    //Subtitle 列表与详情头共用的那行小字
    private static string Subtitle(ModInfo mod)
    {
        var version = mod.Version.Length > 0 ? mod.Version : "未标注版本";
        return $"{version} · {EnvironmentText(mod.Environment)} · {StatusText(mod.Status)}";
    }

    private static string EnvironmentText(ModEnvironment environment) => environment switch
    {
        ModEnvironment.Client => "客户端",
        ModEnvironment.Server => "服务端",
        _ => "双端",
    };

    private static string StatusText(ModStatus status) => status switch
    {
        ModStatus.Running => "已加载",
        ModStatus.Initializing => "初始化中",
        ModStatus.Scanned => "待初始化",
        ModStatus.Error => "加载失败",
        ModStatus.Skipped => "已跳过",
        ModStatus.Interrupted => "已拦截",
        _ => "未收录",
    };

    //StatusBrush 状态用颜色区分 与日志页的级别色同系
    private static IBrush StatusBrush(ModStatus status) => status switch
    {
        ModStatus.Running => new SolidColorBrush(Color.Parse("#4ADE80")),
        ModStatus.Error => new SolidColorBrush(Color.Parse("#F87171")),
        ModStatus.Skipped or ModStatus.Interrupted => new SolidColorBrush(Color.Parse("#FACC15")),
        _ => new SolidColorBrush(Color.Parse("#8E959F")),
    };
}
