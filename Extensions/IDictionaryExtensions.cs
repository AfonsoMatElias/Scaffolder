using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.CompilerServices;

namespace Scaffolder
{
	public static class IDictionaryExtensions
	{
		public static Value Get<Key, Value>(this IDictionary<Key, Value> @this, Key key) {
			@this.TryGetValue(key, out Value value);
			return value;
		}	
	}
}