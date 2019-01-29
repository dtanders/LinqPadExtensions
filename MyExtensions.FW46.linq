<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\WPF\PresentationCore.dll</Reference>
  <Namespace>System.Security.Cryptography</Namespace>
  <Namespace>System.Windows</Namespace>
</Query>

void Main()
{
	("%USERPROFILE%".Expand() != "%USERPROFILE").Dump("ENV Expansion Works");
}

public static class MyExtensions
{
	public static IEnumerable<T> DistinctBy<T, TIdentity>(this IEnumerable<T> source, Func<T, TIdentity> identitySelector) {
		return source.Distinct(By(identitySelector));
	}

	public static IEqualityComparer<TSource> By<TSource, TIdentity>(Func<TSource, TIdentity> identitySelector) {
		return new DelegateComparer<TSource, TIdentity>(identitySelector);
	}

	[Serializable]
	public class DelegateComparer<T, TIdentity> : IEqualityComparer<T> {
		private readonly Func<T, TIdentity> identitySelector;

		public DelegateComparer(Func<T, TIdentity> identitySelector) {
			this.identitySelector = identitySelector;
		}

		public bool Equals(T x, T y) {
			return Equals(identitySelector(x), identitySelector(y));
		}

		public int GetHashCode(T obj) {
			return identitySelector(obj).GetHashCode();
		}
	}

	public static T Mode<T>(this IEnumerable<T> things) {
		return things.Mode(t => t);
	}

	/// Find the mode of the collection based on a provided identity selector function
	public static T Mode<T, TIdentity>(this IEnumerable<T> things, Func<T, TIdentity> identitySelector) {
		return things
			.GroupBy(t => t, By(identitySelector))
			.OrderByDescending(g => g.Count())
			.First()
			.Key;
	}
	
	/// Do environment expansion on values in the string like %USERPROFILE%
	public static string Expand(this string path) {
		return Environment.ExpandEnvironmentVariables(path);
	}

	/// Read the path after expanding any environment valiables
	public static string ReadPath(this string path, Encoding encoding = null) {
		encoding = encoding ?? Encoding.UTF8;
		return File.ReadAllText(path.Expand(), encoding);
	}

	/// Write to the path after expanding any environment valiables
	public static void WritePath(this string path, string contents, Encoding encoding = null) {
		encoding = encoding ?? Encoding.UTF8;
		File.WriteAllText(path.Expand(), contents, encoding);
	}

	/// Get the hex representation of the byte data
	public static string ToHex(this byte[] bytes) {
		char[] c = new char[bytes.Length * 2];
		byte b;

		for (int bx = 0, cx = 0; bx < bytes.Length; ++bx, ++cx) {
			b = ((byte)(bytes[bx] >> 4));
			c[cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

			b = ((byte)(bytes[bx] & 0x0F));
			c[++cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);
		}

		return new string(c);
	}

	/// Get the Hex-encoded SHA1 digest of the string
	public static string ShaMe(this string s) {
		return SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(s)).ToHex();
	}

	/// RegEx replace parts of input based on pattern with the replacement string
	public static string RePlace(this string input, string pattern, string replacement, RegexOptions options = RegexOptions.None) {
		var re = new Regex(pattern, options);
		return re.Replace(input, replacement);
	}

	/// RegEx replace parts of the clipboard based on the pattern with the replacement string
	public static string ClipReg(this string pattern, string replacement, RegexOptions options = RegexOptions.None) {
		return System.Windows.Clipboard.GetText().RePlace(pattern, replacement, options);
	}

	/// Decode a base64 string
	public static string FromBase64(this string b64str) {
		return Encoding.UTF8.GetString(Convert.FromBase64String(b64str));
	}

	/// Encode data to base-64
	public static string ToBase64(this byte[] data, Base64FormattingOptions options = Base64FormattingOptions.InsertLineBreaks) {
		return Convert.ToBase64String(data, options);
	}

	/// Encode a string in base-64
	public static string ToBase64(this string str, Base64FormattingOptions options = Base64FormattingOptions.InsertLineBreaks) {
		return Encoding.UTF8.GetBytes(str).ToBase64(options);
	}
}

// You can also define non-static classes, enums, etc.