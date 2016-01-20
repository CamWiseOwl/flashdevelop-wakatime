﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WakaTime {
    public static class StringExtensions {
        public static bool IsNullOrWhiteSpace(this string value) {
            if (value != null) {
                for (int i = 0; i < value.Length; i++) {
                    if (!char.IsWhiteSpace(value[i])) {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
