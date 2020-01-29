<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\WPF\PresentationCore.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Serialization.dll</Reference>
  <Namespace>System.IO</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
  <Namespace>System.Windows</Namespace>
  <Namespace>System.Runtime.CompilerServices</Namespace>
  <Namespace>System.Runtime.Serialization</Namespace>
  <Namespace>System</Namespace>
</Query>

void Main()
{
	("%USERPROFILE%".Expand() != "%USERPROFILE%").Dump("ENV Expansion Works");
//	("Waldstraße".EqualsICIC("Waldstrasse")).Dump("Case-Insensitive string compairson works");
//	("i".EqualsICIC("İ")).Dump("Turkish is maybe working?");
//	StringComparer.OrdinalIgnoreCase.Equals("i", "İ").Dump();
	((new List<int> {1, 2, 3}).Join(", ") == "1, 2, 3").Dump("Join ints works");
}

public enum SHAVer {
	_1 = 1,
	_256 = 256,
	_512 = 512,
}

public static class MyExtensions
{
	public static Encoding Enc = Encoding.UTF8;
	
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
	
	///Concatenate assignable types because Enumerable.Concat isn't smart enough
	public static IEnumerable<TBase> Concat<TBase, TDerived>(this IEnumerable<TBase> first, IEnumerable<TDerived> second)
		 where TDerived : TBase
	{
		foreach (var thing in first) {
			yield return thing;
		}
		foreach (var thing in second) {
			yield return thing;
		}
	}
	
	/// Wrapper to get indexes in a foreach
	public static IEnumerable<(int i, T t)> Enumerate<T>(this IEnumerable<T> sequence, int startAt=0) {
		foreach (var item in sequence){
			yield return (i: startAt++, t: item);
		}
	}

	/// Wrapper to get very large (ulong) indexes in a foreach
	public static IEnumerable<(ulong i, T t)> EnumerateLarge<T>(this IEnumerable<T> sequence, ulong startAt=0) {
		foreach (var item in sequence) {
			yield return (i: startAt++, t: item);
		}
	}

	/// Do environment expansion on values in the string like %USERPROFILE%
	public static string Expand(this string path) {
		return Environment.ExpandEnvironmentVariables(path);
	}

	/// Read the path after expanding any environment valiables
	public static string ReadPath(this string path, Encoding encoding = null) {
		encoding = encoding ?? Enc;
		return File.ReadAllText(path.Expand(), encoding);
	}

	/// Write to the path after expanding any environment valiables (defaults to Unicode)
	public static void WritePath(this string path, string contents, Encoding encoding = null) {
		encoding = encoding ?? Enc;
		var expandedPath = path.Expand();
		Directory.CreateDirectory(Path.GetFullPath(expandedPath));
		File.WriteAllText(expandedPath, contents, encoding);
	}
	
	///Get the contents of a path
	public static IEnumerable<FileSystemInfo> Dir(this string path, string searchPatternGlob="*", 
		SearchOption searchOption=SearchOption.TopDirectoryOnly)
	{
		var info = new DirectoryInfo(path);
		return Enumerable.Empty<FileSystemInfo>()
			.Concat(info.EnumerateDirectories(searchPatternGlob, searchOption))
			.Concat(info.EnumerateFiles(searchPatternGlob, searchOption));
	}

	///Write a string to a file represented by a file info
	public static FileSystemInfo Write(this FileSystemInfo file, string content, Encoding encoding = null) {
		if (file is FileInfo f) {
			f.FullName.WritePath(content, encoding);
		}
		return file;
	}
	
	public static string Read(this FileSystemInfo file, Encoding encoding=null){
		if (file is FileInfo f){
			return f.FullName.ReadPath(encoding);
		}
		return null;
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

	///Get the SHA digest of the string
	public static byte[] ShaMeToBytes(this string s, SHAVer version = SHAVer._1) {
		var bytes = Enc.GetBytes(s);
		switch (version) {
			case SHAVer._512:
				return System.Security.Cryptography.SHA512.Create().ComputeHash(bytes);
			case SHAVer._256:
				return System.Security.Cryptography.SHA256.Create().ComputeHash(bytes);
			case SHAVer._1:
			default:
				return SHA1.Create().ComputeHash(bytes);
		}
	}

	/// Get the Hex-encoded SHA digest of the string
	public static string ShaMe(this string s, SHAVer version = SHAVer._1) {
		return s.ShaMeToBytes(version).ToHex();
	}
	
	///Get the Base64-encoded SHA digest of the string
	public static string ShaMe64(this string s, SHAVer version = SHAVer._1) {
		return s.ShaMeToBytes(version).ToBase64();
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
		return Enc.GetString(Convert.FromBase64String(b64str));
	}

	/// Encode data to base-64
	public static string ToBase64(this byte[] data, Base64FormattingOptions options = Base64FormattingOptions.InsertLineBreaks) {
		return Convert.ToBase64String(data, options);
	}

	/// Encode a string in base-64
	public static string ToBase64(this string str, Base64FormattingOptions options = Base64FormattingOptions.InsertLineBreaks) {
		return Enc.GetBytes(str).ToBase64(options);
	}
	
	public static byte[] ToBytes(this string str) {
		return Enc.GetBytes(str);
	}
	
	/// Case-insensitive string comparison using Invariant Ignore Case
	public static bool EqualsICIC(this string a, string b){
		return StringComparer.InvariantCultureIgnoreCase.Equals(a, b);
	}
	
	/// LINQ style string join
	public static string Join<T>(this IEnumerable<T> sequence, string separator){
		return string.Join(separator, sequence);
	}
	
	/// Simplistic line wrapping function
	public static string Wrap(this string tooLong, int lineLength=80) {
		return tooLong.RePlace(@"(.{" + lineLength + @",}?\b.)(\b)", "$1$2" + Environment.NewLine);
	}
	
	/// Because IsAssignableFrom doesn't work quite right with generic types
	public static bool IsAssignableFromGeneic(this Type type, Type someGenericType) {
		//maybe also check IsAssignableFrom?
		return type.BaseType != null && type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == someGenericType;
	}

	/// Get the property info out of the body of an Expression<Func<>>
	public static PropertyInfo PropInfo<T>(this Expression<T> exp) 
		where T: Delegate {
		return (PropertyInfo)((MemberExpression)exp.Body).Member;
	}
	
	public static TOut TryMe<TIn, TOut>(this Func<TIn, TOut> func, TIn args){
		try {
			return func(args);
		} catch (Exception ex) {
			ex.Dump();
		}
		return default;
	}

	public static T TryMe<T>(this Func<T> func) {
		try {
			return func();
		} catch (Exception ex) {
			ex.Dump();
		}
		return default;
	}
	
	public static void TryMe<T>(this Action<T> act, T args){
		try {
			act(args);
		} catch (Exception ex) {
			ex.Dump();
		}
	}
	
	public static void TryMe(this Action act) {
		try {
			act();
		} catch (Exception ex) {
			ex.Dump();
		}
	}

	private static Regex imperfectNumberDetector = new Regex(@"^-*[\d\.]+$");
	///Escape parts of the results copied out of SSMS so they can go into an INSERT statement
	public static string SqlFormatter(this string results)
		=> results
			.Split('\t')
			.Select(col => col == "NULL" 
					? col 
					: (!imperfectNumberDetector.IsMatch(col) 
						? $"'{col.Replace("'", "''")}'" 
						: col))
			.Join(", ");

	//	public static IEnumerable<string> AsInsert<T>(this IQueryable<T> records) {
	//		records.ElementType.
	//	}
	
	//from https://stackoverflow.com/a/41263850/301807
	public static IQueryable<T> SetQueryName<T>(this IQueryable<T> source,
	  [CallerMemberName] String name = null,
	  [CallerFilePath] String sourceFilePath = "",
	  [CallerLineNumber] Int32 sourceLineNumber = 0) {
		var expr = Expression.NotEqual(Expression.Constant("Query name: " + name), Expression.Constant(null));
		var param = Expression.Parameter(typeof(T), "param");
		var criteria1 = Expression.Lambda<Func<T, Boolean>>(expr, param);

		expr = Expression.NotEqual(Expression.Constant($"Source: {sourceFilePath} ({sourceLineNumber})"), Expression.Constant(null));
		var criteria2 = Expression.Lambda<Func<T, Boolean>>(expr, param);

		return source.Where(criteria1).Where(criteria2);
	}

	/// <summary>
	/// Clones any object and returns the new cloned object.
	/// From https://stackoverflow.com/a/2178383/301807
	/// </summary>
	/// <typeparam name="T">The type of object.</typeparam>
	/// <param name="source">The original object.</param>
	/// <returns>The clone of the object.</returns>
	public static T Clone<T>(this T source) {
		var dcs = new DataContractSerializer(typeof(T));
		using (var ms = new System.IO.MemoryStream()) {
			dcs.WriteObject(ms, source);
			ms.Seek(0, System.IO.SeekOrigin.Begin);
			return (T)dcs.ReadObject(ms);
		}
	}
}

// You can also define non-static classes, enums, etc.