using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.viddiesToolbox.Menu {
    public class CustomEnumerableSlider<T> : TextMenu.Option<T> {
        public CustomEnumerableSlider(string label, IEnumerable<T> options, Func<T, string> mapping, T startValue)
            : base(label) {
            foreach (T option in options) {
                Add(mapping(option), option, option.Equals(startValue));
            }
        }
    }
}
