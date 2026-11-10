namespace WindowsModern.SnowFall
{
	/// <summary>单个雪花的可变状态。字段而非属性，避免每帧装箱。</summary>
	internal sealed class Snowflake
	{
		public double X;
		public double Y;
		public double Size;
		public double Speed;      // 像素 / 秒
		public double Sway;       // 横向摆幅
		public double Phase;      // 当前摆动相位
		public double PhaseSpeed; // 相位速度 rad / 秒
		public double Opacity;
	}
}