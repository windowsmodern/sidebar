using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sidebar
{
	internal enum ButtonStatus
	{
		Normal = 0x00,
		Focus = 0x01,
		Hover = 0x02,
		Active = 0x03,
		Disabled = 0x04,
		CheckedNormal = 0x08,
		CheckedFocus = 0x09,
		CheckedHover = 0x0A,
		CheckedActive = 0x0B,
		CheckedDisabled = 0x0C
	}
}
