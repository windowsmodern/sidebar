using System;
using System.Windows;
using System.Windows.Controls;

namespace Sidebar
{
	/// <summary>
	/// A composite button that hosts an Options button and a Command button. Hover state is shared across both child buttons; pressed state is tracked per child button.
	/// <para xml:lang="zh-Hans">一个复合按钮，包含 Options 和 Command 两个子按钮。悬停状态在两个子按钮间共享；按下状态按子按钮各自跟踪。</para>
	/// <para xml:lang="zh-Hant">一個複合按鈕，包含 Options 和 Command 兩個子按鈕。懸停狀態在兩個子按鈕間共用；按下狀態按子按鈕各自追蹤。</para>
	/// </summary>
	[TemplatePart (Name = "PART_OptionsButton", Type = typeof (Button))]
	[TemplatePart (Name = "PART_CommandButton", Type = typeof (Button))]
	public class FoldOptionsButton: Button
	{
		/// <summary>
		/// The Options button retrieved from the control template.
		/// <para xml:lang="zh-Hans">从控件模板中获取的 Options 按钮。</para>
		/// <para xml:lang="zh-Hant">從控制項範本中取得的 Options 按鈕。</para>
		/// </summary>
		private Button _optionsButton;
		/// <summary>
		/// The Command button retrieved from the control template.
		/// <para xml:lang="zh-Hans">从控件模板中获取的 Command 按钮。</para>
		/// <para xml:lang="zh-Hant">從控制項範本中取得的 Command 按鈕。</para>
		/// </summary>
		private Button _commandButton;

		/// <summary>
		/// Initializes a new instance of the <see cref="FoldOptionsButton"/> class.
		/// <para xml:lang="zh-Hans">初始化 <see cref="FoldOptionsButton"/> 类的新实例。</para>
		/// <para xml:lang="zh-Hant">初始化 <see cref="FoldOptionsButton"/> 類別的新執行個體。</para>
		/// </summary>
		public FoldOptionsButton ()
		{
			IsTabStop = false;
			Focusable = false;
		}

		/// <summary>
		/// Applies the control template and wires up the child buttons.
		/// <para xml:lang="zh-Hans">应用控件模板并连接子按钮事件。</para>
		/// <para xml:lang="zh-Hant">套用控制項範本並連接子按鈕事件。</para>
		/// </summary>
		public override void OnApplyTemplate ()
		{
			// 解绑旧模板中的按钮，避免模板重应用时重复订阅
			if (_optionsButton != null)
				_optionsButton.Click -= OnOptionsButtonClick;
			if (_commandButton != null)
				_commandButton.Click -= OnCommandButtonClick;

			base.OnApplyTemplate ();

			_optionsButton = GetTemplateChild ("PART_OptionsButton") as Button;
			_commandButton = GetTemplateChild ("PART_CommandButton") as Button;

			if (_optionsButton != null)
				_optionsButton.Click += OnOptionsButtonClick;
			if (_commandButton != null)
				_commandButton.Click += OnCommandButtonClick;
		}

		/// <summary>
		/// Raises the <see cref="OptionsButtonClick"/> event when the Options button is clicked.
		/// <para xml:lang="zh-Hans">当 Options 按钮被点击时，触发 <see cref="OptionsButtonClick"/> 事件。</para>
		/// <para xml:lang="zh-Hant">當 Options 按鈕被點擊時，觸發 <see cref="OptionsButtonClick"/> 事件。</para>
		/// </summary>
		private void OnOptionsButtonClick (object sender, RoutedEventArgs e)
		{
			OptionsButtonClick?.Invoke (this, e);
		}

		/// <summary>
		/// Raises the <see cref="CommandButtonClick"/> event when the Command button is clicked.
		/// <para xml:lang="zh-Hans">当 Command 按钮被点击时，触发 <see cref="CommandButtonClick"/> 事件。</para>
		/// <para xml:lang="zh-Hant">當 Command 按鈕被點擊時，觸發 <see cref="CommandButtonClick"/> 事件。</para>
		/// </summary>
		private void OnCommandButtonClick (object sender, RoutedEventArgs e)
		{
			CommandButtonClick?.Invoke (this, e);
		}

		#region Options Button
		/// <summary>
		/// Identifies the <see cref="OptionsButtonStyle"/> dependency property.
		/// <para xml:lang="zh-Hans">标识 <see cref="OptionsButtonStyle"/> 依赖属性。</para>
		/// <para xml:lang="zh-Hant">識別 <see cref="OptionsButtonStyle"/> 相依性屬性。</para>
		/// </summary>
		public static readonly DependencyProperty OptionsButtonStyleProperty =
			DependencyProperty.Register (nameof (OptionsButtonStyle), typeof (Style), typeof (FoldOptionsButton),
				new PropertyMetadata (null));
		/// <summary>
		/// Gets or sets the style applied to the Options button.
		/// <para xml:lang="zh-Hans">获取或设置应用于 Options 按钮的样式。</para>
		/// <para xml:lang="zh-Hant">取得或設定套用至 Options 按鈕的樣式。</para>
		/// </summary>
		public Style OptionsButtonStyle
		{
			get { return (Style)GetValue (OptionsButtonStyleProperty); }
			set { SetValue (OptionsButtonStyleProperty, value); }
		}
		/// <summary>
		/// Identifies the <see cref="OptionsButtonContent"/> dependency property.
		/// <para xml:lang="zh-Hans">标识 <see cref="OptionsButtonContent"/> 依赖属性。</para>
		/// <para xml:lang="zh-Hant">識別 <see cref="OptionsButtonContent"/> 相依性屬性。</para>
		/// </summary>
		public static readonly DependencyProperty OptionsButtonContentProperty =
			DependencyProperty.Register (nameof (OptionsButtonContent), typeof (object), typeof (FoldOptionsButton),
				new PropertyMetadata (null));
		/// <summary>
		/// Gets or sets the content displayed by the Options button.
		/// <para xml:lang="zh-Hans">获取或设置 Options 按钮显示的内容。</para>
		/// <para xml:lang="zh-Hant">取得或設定 Options 按鈕顯示的內容。</para>
		/// </summary>
		public object OptionsButtonContent
		{
			get { return GetValue (OptionsButtonContentProperty); }
			set { SetValue (OptionsButtonContentProperty, value); }
		}
		/// <summary>
		/// Occurs when the Options button is clicked.
		/// <para xml:lang="zh-Hans">当 Options 按钮被点击时发生。</para>
		/// <para xml:lang="zh-Hant">當 Options 按鈕被點擊時發生。</para>
		/// </summary>
		public event RoutedEventHandler OptionsButtonClick;
		#endregion

		#region Command Button
		/// <summary>
		/// Identifies the <see cref="CommandButtonStyle"/> dependency property.
		/// <para xml:lang="zh-Hans">标识 <see cref="CommandButtonStyle"/> 依赖属性。</para>
		/// <para xml:lang="zh-Hant">識別 <see cref="CommandButtonStyle"/> 相依性屬性。</para>
		/// </summary>
		public static readonly DependencyProperty CommandButtonStyleProperty =
			DependencyProperty.Register (nameof (CommandButtonStyle), typeof (Style), typeof (FoldOptionsButton),
				new PropertyMetadata (null));
		/// <summary>
		/// Gets or sets the style applied to the Command button.
		/// <para xml:lang="zh-Hans">获取或设置应用于 Command 按钮的样式。</para>
		/// <para xml:lang="zh-Hant">取得或設定套用至 Command 按鈕的樣式。</para>
		/// </summary>
		public Style CommandButtonStyle
		{
			get { return (Style)GetValue (CommandButtonStyleProperty); }
			set { SetValue (CommandButtonStyleProperty, value); }
		}
		/// <summary>
		/// Identifies the <see cref="CommandButtonContent"/> dependency property.
		/// <para xml:lang="zh-Hans">标识 <see cref="CommandButtonContent"/> 依赖属性。</para>
		/// <para xml:lang="zh-Hant">識別 <see cref="CommandButtonContent"/> 相依性屬性。</para>
		/// </summary>
		public static readonly DependencyProperty CommandButtonContentProperty =
			DependencyProperty.Register (nameof (CommandButtonContent), typeof (object), typeof (FoldOptionsButton),
				new PropertyMetadata (null));
		/// <summary>
		/// Gets or sets the content displayed by the Command button.
		/// <para xml:lang="zh-Hans">获取或设置 Command 按钮显示的内容。</para>
		/// <para xml:lang="zh-Hant">取得或設定 Command 按鈕顯示的內容。</para>
		/// </summary>
		public object CommandButtonContent
		{
			get { return GetValue (CommandButtonContentProperty); }
			set { SetValue (CommandButtonContentProperty, value); }
		}
		/// <summary>
		/// Occurs when the Command button is clicked.
		/// <para xml:lang="zh-Hans">当 Command 按钮被点击时发生。</para>
		/// <para xml:lang="zh-Hant">當 Command 按鈕被點擊時發生。</para>
		/// </summary>
		public event RoutedEventHandler CommandButtonClick;
		#endregion
	}
}