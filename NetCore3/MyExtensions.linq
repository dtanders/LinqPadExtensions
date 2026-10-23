<Query Kind="Program">
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Net.Http.Json</Namespace>
  <Namespace>System.Runtime.CompilerServices</Namespace>
  <Namespace>System.Runtime.Serialization</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.IO.Compression</Namespace>
  <Namespace>System.Text.Encodings.Web</Namespace>
</Query>

void Main()
{
	("%USERPROFILE%".Expand() != "%USERPROFILE%").Dump("ENV Expansion Works");
	//("Waldstraße".EqualsICIC("Waldstrasse")).Dump("Case-Insensitive string compairson works");
	//("i".EqualsICIC("İ")).Dump("Turkish is maybe working?");
	//StringComparer.OrdinalIgnoreCase.Equals("i", "İ").Dump();
	((new List<int> { 1, 2, 3 }).Join(", ") == "1, 2, 3").Dump("Join ints works");
#if NET6
	"SQL:EXEC Foo @Json='{\"id\":1,\"name\":\"x\"}'".Decode().Dump("Decode works");
#endif
}

public enum SHAVer {
	_1 = 1,
	_256 = 256,
	_512 = 512,
}

public static class MyExtensions {
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
	
	public static List<T> Append<T>(this List<T> someList, IEnumerable<T> someOtherList){
		someList.AddRange(someOtherList);
		return someList;
	}

	/// Wrapper to get indexes in a foreach with less typing than Select
	public static IEnumerable<(uint i, T t)> Enumerate<T>(this IEnumerable<T> sequence, uint startAt=0) {
		foreach (var item in sequence) {
			yield return (i: checked(startAt++), t: item);
		}
	}

	/// Wrapper to get very large (ulong) indexes in a foreach
	public static IEnumerable<(ulong i, T t)> EnumerateLarge<T>(this IEnumerable<T> sequence, ulong startAt=0) {
		checked {
			foreach (var item in sequence) {
				yield return (i: checked(startAt++), t: item);
			}
		}
	}
	
	/// WIP-ish
	public static IEnumerable<T> Enumerateable<T>(this IEnumerable enumerable){
		foreach (T element in enumerable) {
			yield return element;
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
	
	/// Read the path as an enumerable of lines after expanding any environment valiables
	public static IEnumerable<string> ReadPathLines(this string path, Encoding encoding = null){
		encoding = encoding ?? Enc;
		return File.ReadLines(path.Expand(), encoding);
	}
	
	/// Read the path and return all bytes after expanding any environment variables
	public static byte[] ReadPathBytes(this string path) {
		return File.ReadAllBytes(path.Expand());
	}

	/// Write to the path after expanding any environment valiables (defaults to Unicode)
	public static void WritePath(this string path, string contents, Encoding encoding = null) {
		encoding = encoding ?? Enc;
		var expandedPath = path.Expand();
		Directory.CreateDirectory(Path.GetDirectoryName(expandedPath));
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

	public static string Read(this FileSystemInfo file, Encoding encoding=null) {
		if (file is FileInfo f) {
			return f.FullName.ReadPath(encoding);
		}
		return null;
	}

	/// Get the hex representation of the byte data
	public static string ToHex(this byte[] bytes) => new string(bytes.ToHexChars());

	public static char[] ToHexChars(this byte[] bytes) {
		char[] c = new char[bytes.Length * 2];
		byte b;

		for (int bx = 0, cx = 0; bx < bytes.Length; ++bx, ++cx) {
			b = ((byte)(bytes[bx] >> 4));
			c[cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

			b = ((byte)(bytes[bx] & 0x0F));
			c[++cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);
		}
		return c;
	}

	/// Hash some dang bytes
	public static byte[] ShaBytes(this byte[] bytes, SHAVer version = SHAVer._1) {
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

	/// Get the SHA digest of the string with our default encoding
	public static byte[] ShaMeToBytes(this string s, SHAVer version = SHAVer._1) {
		return Enc.GetBytes(s).ShaBytes(version);
	}

	/// Get the Hex-encoded SHA digest of the string
	public static string ShaMe(this string s, SHAVer version = SHAVer._1) {
		return s.ShaMeToBytes(version).ToHex();
	}

	/// Get the Base64-encoded SHA digest of the string
	public static string ShaMe64(this string s, SHAVer version = SHAVer._1) {
		return s.ShaMeToBytes(version).ToBase64();
	}

	/// RegEx replace parts of input based on pattern with the replacement string
	public static string RePlace(this string input, string pattern, string replacement, RegexOptions options = RegexOptions.None) {
		var re = new Regex(pattern, options);
		return re.Replace(input, replacement);
	}

#if WINDOWS 
	/// RegEx replace parts of the clipboard based on the pattern with the replacement string
	public static string ClipReg(this string pattern, string replacement, RegexOptions options = RegexOptions.None) {
		return System.Windows.Clipboard.GetText().RePlace(pattern, replacement, options);
	}
#endif

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
	public static bool EqualsICIC(this string a, string b) {
		return StringComparer.InvariantCultureIgnoreCase.Equals(a, b);
	}

	/// LINQ style string join
	public static string Join<T>(this IEnumerable<T> sequence, string separator) {
		return string.Join(separator, sequence);
	}

	/// LINQ join with formatting function
	public static string Join<T>(this IEnumerable<T> sequence, string seperator, Func<T, string> formatter) {
		return sequence.Select(s => formatter(s)).Join(seperator);
	}

	/// <summary>Never know when you need to randomly pick something</summary>
	public static T TakeRandom<T>(this IEnumerable<T> source) {
		return source.TakeRandom(1).Single();
	}

	public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> source, int count) {
		return source.Shuffle().Take(count);
	}

	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) {
		return source.OrderBy(x => Guid.NewGuid());
	}

	/// Simplistic line wrapping function
	public static string Wrap(this string tooLong, int lineLength = 80) {
		return tooLong.RePlace(@"(.{" + lineLength + @",}?\b.)(\b)", "$1$2" + Environment.NewLine);
	}

	public static bool ContainsIgnoreCase(this string s, string search)
		=> s.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1;

	/// Convert and return as the type
	public static T ConvertObject<T>(object input) {
		return (T)Convert.ChangeType(input, typeof(T));
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

	public static bool IsDefault<T>(this T obj) =>
		default(T) == null
			? obj == null
			: default(T).Equals(obj);

	public static TOut TryMe<TIn, TOut>(this Func<TIn, TOut> func, TIn args) {
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

	public static void TryMe<T>(this Action<T> act, T args) {
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

	///this is probably a terrible idea
	public static IEnumerable<T> Yield<T>(this Func<bool> condition, Func<T> generator) {
		while (condition()) {
			yield return generator();
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

	public static string FormatXml(this string xml) {
		try {
			XDocument doc = XDocument.Parse(xml);
			return doc.ToString();
		} catch (Exception x) {
			$"Problem formatting the XML: {x.Message}".Dump();
			// Handle and throw if fatal exception here; don't just ignore them
			return xml;
		}
	}

#if NET6
	/// Detect and pretty-print JSON / XML / SQL / base64 / url-encoded payloads
	/// nested anywhere in the text. Dumps as a collapsible tree.
	public static PayloadDecoder.Node Decode(this string text) => PayloadDecoder.Decoder.Decode(text);

	/// Decode and dump, painting every case-insensitive match of `highlight`;
	/// returns the original string so it can sit in the middle of a chain
	public static string DumpDecoded(this string text, string heading = null, string highlight = null) {
		var marker = string.IsNullOrEmpty(highlight) ? null : new Regex(Regex.Escape(highlight), RegexOptions.IgnoreCase);
		var html = Util.RawHtml(PayloadDecoder.Renderer.Render(PayloadDecoder.Decoder.Decode(text), marker));
		if (heading == null) html.Dump(); else html.Dump(heading);
		return text;
	}
#endif

	public static string DumpCopyable(this string str, string header="") {
		new XElement("LINQPad.HTML",
			new XElement("div",
				new XAttribute("class", "headingpresenter"),
				new XElement("h1",
					new XAttribute("class", "headingpresenter"),
					header,
					new XElement("span",
						new XAttribute("onclick", "(function(e) { "
							+ "var t = event.target.parentNode.parentNode.querySelector('.copytarget');"
							+ "var range = document.body.createTextRange();"
							+ "range.moveToElementText(t);"
							+ "range.select(); range.execCommand('Copy');"
							+ "return false; })(event)"),
						new XAttribute("style", "cursor: pointer; margin-left: .25em"),
						new XAttribute("title", "Copy to Clipboard"),
						"📄"
					)
				),
				new XElement("div",
					new XAttribute("class", "copytarget"),
					str //doesn't like new lines for some reason
				)
			)
		).Dump();
		return str;
	}
}

#if NET6
// =============================================================================
// Payload decoder - lifted from hqLogExplorer so any query can use it.
// Finds JSON / XML / SQL / base64 (incl. gzip/zlib) / url-encoded payloads
// nested inside arbitrary text, in any combination, pretty-prints them, and
// renders a collapsible HTML tree. Usage:
//     someString.Decode()                         // dumps as the tree
//     someString.DumpDecoded("heading", "term")   // dump with matches painted
// Kept in one class so its Node/Ident/Decoder/Renderer names don't collide
// with types in the queries that load My Extensions.
// =============================================================================

public static class PayloadDecoder
{

	// What identifies a record in these payloads, in the order it reads best.
	// `code` earns its place because a policy's entitlements all repeat the policy's
	// own id and differ only by code - without it their rules payloads are
	// indistinguishable.
	public struct Ident
	{
		public string Name, Code, Id;
		public bool IsEmpty => Name == null && Code == null && Id == null;

		// fill blanks from an enclosing object; the closest value for each field wins
		public Ident Fill(Ident outer) => new Ident
		{
			Name = Name ?? outer.Name,
			Code = Code ?? outer.Code,
			Id = Id ?? outer.Id,
		};

		public override string ToString() =>
			string.Join("  ", new[] { Name, Code, Id }.Where(p => p != null));
	}

	public class Node
	{
		public string Kind;
		public string Label;
		public Ident Own;      // what this payload calls itself
		public Ident Parent;   // what the JSON object it was lifted out of calls itself
		public string Text;
		public List<Node> Kids = new List<Node>();
		public bool Structured => Kind != "text";
		public object ToDump() => Util.RawHtml(Renderer.Render(this));
	}

	public static class Decoder
	{
		const int MaxDepth = 6;
		const int MaxNodes = 400;
		const int MaxScan = 2_000_000;
		static int _nodes;

		static readonly Regex Base64Rx = new Regex(
			@"(?<![A-Za-z0-9+/=])[A-Za-z0-9+/]{40,}={0,2}(?![A-Za-z0-9+/=])", RegexOptions.Compiled);
		static readonly Regex PctRx = new Regex(@"%[0-9A-Fa-f]{2}", RegexOptions.Compiled);
		static readonly Regex SqlHeadRx = new Regex(
			@"^\s*(EXEC(UTE)?|SELECT|INSERT\s+INTO|UPDATE|DELETE\s+FROM|DECLARE|MERGE|WITH|CREATE|ALTER)\b",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);

		// decode arbitrary text into a payload tree; `root` lets a caller supply
		// its own top node (with kids of its own already in place) to decode into
		public static Node Decode(string text, Node root = null)
		{
			_nodes = 0;
			text ??= "";
			root ??= new Node { Kind = "decoded", Label = $"{text.Length:n0} chars" };

			var body = text.Length > MaxScan ? text.Substring(0, MaxScan) : text;
			root.Kids.AddRange(ScanParts(body, 0));
			if (text.Length > MaxScan)
				root.Kids.Add(new Node { Kind = "text", Text = $"… {text.Length - MaxScan:n0} more characters not scanned" });

			// done as a pass over the finished tree rather than threaded through the
			// scan, so no decoder signature has to carry ancestry
			AttachParentTags(root, null);
			return root;
		}

		public static List<Node> ScanParts(string s, int depth)
		{
			var parts = new List<Node>();
			if (string.IsNullOrEmpty(s)) return parts;
			if (depth > MaxDepth || _nodes > MaxNodes) { AddText(parts, s, depth); return parts; }

			var b64 = Base64Rx.Matches(s);
			int bi = 0, i = 0, textStart = 0;

			while (i < s.Length)
			{
				Node found = null;
				int segEnd = i;
				var c = s[i];

				if (c == '{' || c == '[')
				{
					if (TryJson(s, i, depth, out var end, out var node)) { found = node; segEnd = end; }
				}
				if (found == null && c == '<')
				{
					if (TryXml(s, i, depth, out var end, out var node)) { found = node; segEnd = end; }
				}
				// "SQL:" claims the rest of the string so MakeSql sees whole @param
				// values; a "SQL:" inside a JSON/XML payload is never reached
				// because that payload was consumed as one segment above
				if (found == null && c == 'S' && depth <= MaxDepth &&
					string.CompareOrdinal(s, i, "SQL:", 0, 4) == 0 && s.Length - i > 14)
				{
					found = MakeSql(s.Substring(i + 4), depth + 1);
					segEnd = s.Length;
				}
				if (found == null && c != ' ')
				{
					while (bi < b64.Count && b64[bi].Index < i) bi++;
					if (bi < b64.Count && b64[bi].Index == i && TryBase64(b64[bi].Value, depth, out var node))
					{
						found = node;
						segEnd = i + b64[bi].Length;
					}
				}

				if (found != null)
				{
					if (i > textStart) AddText(parts, s.Substring(textStart, i - textStart), depth);
					parts.Add(found);
					_nodes++;
					i = segEnd;
					textStart = i;
					if (_nodes > MaxNodes) break;
				}
				else i++;
			}

			if (textStart < s.Length) AddText(parts, s.Substring(textStart), depth);
			return parts;
		}

		// plain-text leaves still get a look for SQL and url-encoding
		static void AddText(List<Node> parts, string text, int depth)
		{
			if (text == null || text.Trim().Length == 0) return;

			// bare statements with no "SQL:" marker (that case is handled in ScanParts)
			if (depth <= MaxDepth && SqlHeadRx.IsMatch(text) && text.Trim().Length > 30)
			{
				parts.Add(MakeSql(text, depth + 1));
				return;
			}

			if (depth <= MaxDepth && PctRx.Matches(text).Count >= 3)
			{
				string decoded = null;
				try { decoded = System.Net.WebUtility.UrlDecode(text); } catch { }
				if (decoded != null && decoded != text)
				{
					var node = new Node { Kind = "urlenc", Label = $"url-encoded ({text.Length:n0} chars)" };
					node.Kids.AddRange(ScanParts(decoded, depth + 1));
					if (node.Kids.Count > 1 || node.Kids.Any(k => k.Structured))
					{
						parts.Add(node);
						return;
					}
					// nothing structured inside: just show the readable form
					parts.Add(new Node { Kind = "urlenc", Label = "url-decoded", Text = decoded });
					return;
				}
			}

			AddPlain(parts, text);
		}

		static void AddPlain(List<Node> parts, string text)
		{
			if (text == null || text.Trim().Length == 0) return;
			parts.Add(new Node { Kind = "text", Text = text.Trim('\n').TrimEnd() });
		}

		// ── JSON ──
		static readonly JsonDocumentOptions JsonOpts = new JsonDocumentOptions
		{
			AllowTrailingCommas = true,
			CommentHandling = JsonCommentHandling.Skip,
			MaxDepth = 256,
		};
		static readonly JsonSerializerOptions PrettyJson = new JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		};

		static bool TryJson(string s, int start, int depth, out int end, out Node node)
		{
			end = start;
			node = null;

			int braces = 0, i = start;
			bool inString = false, escaped = false;
			for (; i < s.Length; i++)
			{
				var c = s[i];
				if (inString)
				{
					if (escaped) escaped = false;
					else if (c == '\\') escaped = true;
					else if (c == '"') inString = false;
					continue;
				}
				if (c == '"') { inString = true; continue; }
				if (c == '{' || c == '[') braces++;
				else if (c == '}' || c == ']')
				{
					braces--;
					if (braces == 0) { i++; break; }
					if (braces < 0) return false;
				}
			}
			if (braces != 0) return false;

			var candidate = s.Substring(start, i - start);
			if (candidate.Length < 14) return false;
			if (candidate[0] == '{' && !candidate.Contains(':')) return false;
			if (candidate[0] == '[' && !candidate.Contains(',') && !candidate.Contains(':')) return false;

			try
			{
				using var doc = JsonDocument.Parse(candidate, JsonOpts);
				var own = JsonIdent(doc.RootElement);
				node = new Node
				{
					Kind = "json",
					Own = own,
					Label = $"JSON ({candidate.Length:n0} chars)" + (own.IsEmpty ? "" : "  " + own),
					Text = JsonSerializer.Serialize(doc.RootElement, PrettyJson),
				};
				CollectJson(doc.RootElement, "", depth + 1, node.Kids);
				end = i;
				return true;
			}
			catch (JsonException) { return false; }
		}

		// a collapsed JSON node otherwise only says how big it is, so hoist whatever
		// identifies the record into the label. An exact "name"/"code"/"id" wins;
		// failing that, the first camelCase/snake_case suffixed form (auditId,
		// customerName) is used, since plenty of payloads only carry those.
		static Ident JsonIdent(JsonElement root)
		{
			var obj = root;
			if (obj.ValueKind == JsonValueKind.Array)
				foreach (var first in obj.EnumerateArray()) { obj = first; break; }
			if (obj.ValueKind != JsonValueKind.Object) return default;

			var ident = new Ident();
			string nameAlt = null, idAlt = null;
			foreach (var p in obj.EnumerateObject())
			{
				var v = Scalar(p.Value);
				if (v == null) continue;
				if (ident.Name == null && Exact(p.Name, "name")) ident.Name = "name=" + v;
				// only an exact "code" - the suffixed forms in these logs are
				// errorReturnCode, postalCode, colorCode and friends, which say
				// nothing about which record this is
				else if (ident.Code == null && Exact(p.Name, "code")) ident.Code = "code=" + v;
				else if (ident.Id == null && Exact(p.Name, "id")) ident.Id = "id=" + v;
				else if (nameAlt == null && EndsWithField(p.Name, "name")) nameAlt = p.Name + "=" + v;
				else if (idAlt == null && EndsWithField(p.Name, "id")) idAlt = p.Name + "=" + v;
			}

			ident.Name ??= nameAlt;
			ident.Id ??= idAlt;
			return ident;
		}

		static bool Exact(string key, string field) =>
			string.Equals(key, field, StringComparison.OrdinalIgnoreCase);

		// A nested payload is only meaningful in context: "id=1" on a rules payload
		// says nothing, "id=1 ← in code=AchBatch id=G-85" places it. `Parent` (the
		// object the payload was lifted out of) beats anything further up the node
		// tree; an unidentified payload in between does not break the chain.
		static void AttachParentTags(Node n, string inherited)
		{
			var own = n.Own.IsEmpty ? null : n.Own.ToString();
			var parent = n.Parent.IsEmpty ? inherited : n.Parent.ToString();

			if (n.Kind == "json" && parent != null && parent != own)
				n.Label += "  ← in " + parent;

			foreach (var kid in n.Kids) AttachParentTags(kid, own ?? parent);
		}

		// "auditId" and "audit_id" count, "valid" and "paid" do not - the suffix has
		// to start its own word
		static bool EndsWithField(string key, string field)
		{
			if (key.Length <= field.Length) return false;
			if (!key.EndsWith(field, StringComparison.OrdinalIgnoreCase)) return false;
			var boundary = key[key.Length - field.Length];
			var before = key[key.Length - field.Length - 1];
			return char.IsUpper(boundary) || before == '_';
		}

		static string Scalar(JsonElement el)
		{
			switch (el.ValueKind)
			{
				case JsonValueKind.String:
					var s = el.GetString();
					return string.IsNullOrWhiteSpace(s) ? null : Trunc(OneLine(s), 40);
				case JsonValueKind.Number: return el.GetRawText();
				// booleans, nulls, objects and arrays never identify a record -
				// "site_id=false" is noise in a label
				default: return null;
			}
		}

		static void CollectJson(JsonElement el, string path, int depth, List<Node> kids)
		{
			if (kids.Count > 30 || depth > MaxDepth || _nodes > MaxNodes) return;
			switch (el.ValueKind)
			{
				case JsonValueKind.Object:
					// Payloads are lifted out of the document into one flat list, so a
					// nested node has no JSON ancestor to inherit from. Stamp the ones
					// this object produced with its identifier, innermost first - an
					// outer object only fills in what a closer one left blank. The tag
					// is computed only for objects that actually yielded a payload,
					// which keeps it off the hot path for big documents.
					var before = kids.Count;
					foreach (var p in el.EnumerateObject())
						CollectJson(p.Value, path.Length == 0 ? p.Name : path + "." + p.Name, depth, kids);
					if (kids.Count > before)
					{
						var ident = JsonIdent(el);
						if (!ident.IsEmpty)
							for (int k = before; k < kids.Count; k++)
								kids[k].Parent = kids[k].Parent.Fill(ident);
					}
					break;
				case JsonValueKind.Array:
					int n = 0;
					foreach (var item in el.EnumerateArray())
					{
						CollectJson(item, $"{path}[{n}]", depth, kids);
						if (++n > 50) break;
					}
					break;
				case JsonValueKind.String:
					var s = el.GetString();
					if (s != null && s.Length >= 24 && LooksEncoded(s)) AddNested(kids, path, s, depth);
					break;
			}
		}

		// ── XML ──
		static bool TryXml(string s, int start, int depth, out int end, out Node node)
		{
			end = start;
			node = null;

			var i = start;
			var declaration = "";
			if (string.CompareOrdinal(s, i, "<?xml", 0, 5) == 0)
			{
				var close = s.IndexOf("?>", i, StringComparison.Ordinal);
				if (close < 0) return false;
				declaration = s.Substring(i, close + 2 - i);
				i = close + 2;
				while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
				if (i >= s.Length || s[i] != '<') return false;
			}

			var open = Regex.Match(s.Substring(i, Math.Min(256, s.Length - i)), @"^<(?<name>[A-Za-z_][\w.\-]*(?::[A-Za-z_][\w.\-]*)?)(?=[\s/>])");
			if (!open.Success) return false;
			var name = open.Groups["name"].Value;

			// walk forward counting <name …> / </name>, skipping comments and CDATA
			int level = 0, j = i, bodyEnd = -1;
			while (j < s.Length)
			{
				var lt = s.IndexOf('<', j);
				if (lt < 0) break;
				if (string.CompareOrdinal(s, lt, "<!--", 0, 4) == 0)
				{
					var e2 = s.IndexOf("-->", lt, StringComparison.Ordinal);
					if (e2 < 0) break;
					j = e2 + 3;
					continue;
				}
				if (string.CompareOrdinal(s, lt, "<![CDATA[", 0, 9) == 0)
				{
					var e2 = s.IndexOf("]]>", lt, StringComparison.Ordinal);
					if (e2 < 0) break;
					j = e2 + 3;
					continue;
				}
				var gt = s.IndexOf('>', lt);
				if (gt < 0) break;
				var tag = s.Substring(lt, gt - lt + 1);
				var isClose = tag.StartsWith("</", StringComparison.Ordinal);
				var isSelf = tag.EndsWith("/>", StringComparison.Ordinal);
				var tagName = Regex.Match(tag, @"^</?\s*(?<n>[A-Za-z_][\w.\-]*(?::[A-Za-z_][\w.\-]*)?)").Groups["n"].Value;
				if (tagName == name)
				{
					if (isClose) { level--; if (level == 0) { bodyEnd = gt + 1; break; } }
					else if (!isSelf) level++;
					else if (level == 0) { bodyEnd = gt + 1; break; }
				}
				j = gt + 1;
			}
			if (bodyEnd < 0) return false;

			var candidate = s.Substring(i, bodyEnd - i);
			if (candidate.Length < 24) return false;

			try
			{
				var root = XElement.Parse(candidate, LoadOptions.PreserveWhitespace);
				var pretty = (declaration.Length > 0 ? declaration + "\n" : "") + root.ToString();
				node = new Node
				{
					Kind = "xml",
					Label = $"XML <{name}> ({candidate.Length:n0} chars)",
					Text = pretty,
				};
				CollectXml(root, depth + 1, node.Kids);
				end = bodyEnd;
				return true;
			}
			catch (System.Xml.XmlException) { return false; }
		}

		static void CollectXml(XElement el, int depth, List<Node> kids)
		{
			if (depth > MaxDepth || _nodes > MaxNodes) return;
			foreach (var e in el.DescendantsAndSelf())
			{
				if (kids.Count > 30) return;
				foreach (var a in e.Attributes())
					if (a.Value.Length >= 24 && LooksEncoded(a.Value))
						AddNested(kids, $"{Path(e)}/@{a.Name.LocalName}", a.Value, depth);

				foreach (var t in e.Nodes().OfType<XText>())
					if (t.Value.Length >= 24 && LooksEncoded(t.Value))
						AddNested(kids, Path(e), t.Value, depth);
			}
		}

		static string Path(XElement e)
		{
			var names = new List<string>();
			for (var x = e; x != null; x = x.Parent) names.Insert(0, x.Name.LocalName);
			return string.Join("/", names);
		}

		// ── base64 ──
		static bool TryBase64(string token, int depth, out Node node)
		{
			node = null;
			if (token.Length < 40 || token.Length % 4 != 0) return false;

			bool hasUpper = false, hasLower = false, hasDigit = false, hasSymbol = false;
			foreach (var c in token)
			{
				if (c >= 'A' && c <= 'Z') hasUpper = true;
				else if (c >= 'a' && c <= 'z') hasLower = true;
				else if (c >= '0' && c <= '9') hasDigit = true;
				else hasSymbol = true;
			}
			// plain identifiers / hex digests look like base64 but decode to noise
			if (!hasSymbol && !(hasUpper && hasLower && hasDigit)) return false;

			byte[] data;
			try { data = Convert.FromBase64String(token); } catch (FormatException) { return false; }
			if (data.Length < 16) return false;

			var label = $"base64 ({token.Length:n0} chars → {Bytes(data.Length)})";

			// compressed?
			byte[] inflated = null;
			string how = null;
			if (data.Length > 2 && data[0] == 0x1f && data[1] == 0x8b) { inflated = Inflate(data, true); how = "gzip"; }
			else if (data.Length > 2 && data[0] == 0x78) { inflated = Inflate(data, false); how = "zlib"; }
			if (inflated != null)
			{
				node = new Node { Kind = "base64", Label = $"{label}, {how} → {Bytes(inflated.Length)}" };
				if (TryText(inflated, out var inner))
					node.Kids.AddRange(ScanParts(inner, depth + 1));
				else
					node.Kids.Add(new Node { Kind = "binary", Label = "decompressed bytes", Text = Hex(inflated) });
				return true;
			}

			// image?
			var mime = ImageMime(data);
			if (mime != null)
			{
				node = new Node { Kind = "image", Label = $"{label}, {mime}", Text = $"data:{mime};base64,{token}" };
				return true;
			}

			// text?
			if (TryText(data, out var text))
			{
				node = new Node { Kind = "base64", Label = label };
				var kids = ScanParts(text, depth + 1);
				if (kids.Count == 1 && !kids[0].Structured) node.Text = text;
				else node.Kids.AddRange(kids);
				return true;
			}

			// opaque bytes - only worth surfacing when it is clearly a payload
			if (token.Length < 200) return false;
			node = new Node { Kind = "binary", Label = $"{label}, binary", Text = Hex(data) };
			return true;
		}

		static byte[] Inflate(byte[] data, bool gzip)
		{
			try
			{
				using var input = new MemoryStream(data);
				using Stream z = gzip ? new GZipStream(input, CompressionMode.Decompress)
									  : new ZLibStream(input, CompressionMode.Decompress);
				using var output = new MemoryStream();
				z.CopyTo(output, 81920);
				return output.Length == 0 ? null : output.ToArray();
			}
			catch { return null; }
		}

		static bool TryText(byte[] data, out string text)
		{
			text = null;
			try { text = new UTF8Encoding(false, true).GetString(data); }
			catch (DecoderFallbackException) { return false; }
			var printable = text.Count(c => c >= ' ' || c == '\n' || c == '\r' || c == '\t');
			if (printable < text.Length * 0.95) { text = null; return false; }
			return true;
		}

		static string ImageMime(byte[] d)
		{
			if (d.Length > 8 && d[0] == 0x89 && d[1] == 'P' && d[2] == 'N' && d[3] == 'G') return "image/png";
			if (d.Length > 3 && d[0] == 0xff && d[1] == 0xd8 && d[2] == 0xff) return "image/jpeg";
			if (d.Length > 6 && d[0] == 'G' && d[1] == 'I' && d[2] == 'F') return "image/gif";
			if (d.Length > 4 && d[0] == '%' && d[1] == 'P' && d[2] == 'D' && d[3] == 'F') return "application/pdf";
			return null;
		}

		static string Hex(byte[] data)
		{
			var take = Math.Min(data.Length, 512);
			var sb = new StringBuilder();
			for (int i = 0; i < take; i += 16)
			{
				var len = Math.Min(16, take - i);
				sb.Append(i.ToString("x8")).Append("  ");
				for (int j = 0; j < 16; j++)
					sb.Append(j < len ? data[i + j].ToString("x2") + " " : "   ");
				sb.Append(' ');
				for (int j = 0; j < len; j++)
				{
					var c = (char)data[i + j];
					sb.Append(c >= ' ' && c < (char)127 ? c : '.');
				}
				sb.Append('\n');
			}
			if (data.Length > take) sb.Append($"… {data.Length - take:n0} more bytes");
			return sb.ToString();
		}

		// ── SQL ──
		static readonly string[] SqlBreaks =
		{
			"SELECT", "FROM", "WHERE", "INNER JOIN", "LEFT OUTER JOIN", "LEFT JOIN", "RIGHT JOIN",
			"CROSS JOIN", "JOIN", "GROUP BY", "ORDER BY", "HAVING", "VALUES", "INSERT INTO",
			"UPDATE", "SET", "DELETE FROM", "UNION ALL", "UNION", "EXEC", "EXECUTE", "DECLARE",
			"BEGIN", "END", "AND", "OR", "OUTPUT",
		};

		static Node MakeSql(string sql, int depth)
		{
			var literals = new List<string>();
			// mask quoted literals so keyword formatting can never touch their contents
			var masked = Regex.Replace(sql, @"N?'(?:[^']|'')*'", m =>
			{
				literals.Add(m.Value);
				return "\u0001" + (literals.Count - 1) + "\u0002";
			});

			var kids = new List<Node>();
			masked = Regex.Replace(masked, @"@(?<n>\w+)(?<t>\s+\w+)?\s*=\s*(?<v>\u0001\d+\u0002|[^,;\s)]+)", m =>
			{
				var raw = Unmask(m.Groups["v"].Value, literals);
				var value = Unquote(raw);
				if (value.Length >= 400 || (value.Length >= 24 && LooksEncoded(value)))
				{
					var node = new Node
					{
						Kind = "sqlparam",
						Label = $"@{m.Groups["n"].Value} ({value.Length:n0} chars)",
					};
					node.Kids.AddRange(ScanParts(value, depth + 1));
					if (node.Kids.Any(k => k.Structured) || value.Length >= 400)
					{
						kids.Add(node);
						_nodes++;
						return $"@{m.Groups["n"].Value}{m.Groups["t"].Value} = «see @{m.Groups["n"].Value} below»";
					}
				}
				return m.Value;
			});

			var pretty = FormatSql(masked);
			pretty = Unmask(pretty, literals);

			var result = new Node { Kind = "sql", Label = $"SQL ({sql.Trim().Length:n0} chars)", Text = pretty };
			result.Kids.AddRange(kids);
			return result;
		}

		static string FormatSql(string masked)
		{
			var s = Regex.Replace(masked.Trim(), @"[ \t]+", " ");
			s = Regex.Replace(s, @"\s*;\s*", ";\n");
			if (s.Length > 110)
			{
				foreach (var kw in SqlBreaks)
					s = Regex.Replace(s, $@"(?<![\n@\w]){Regex.Escape(kw)}\b", "\n" + kw, RegexOptions.IgnoreCase);
				s = Regex.Replace(s, @"(?<=\S) (@\w+\s*=)", "\n    $1");
				s = Regex.Replace(s, @",\s*(@\w+)", ",\n    $1");
			}
			return Regex.Replace(s, @"\n{2,}", "\n").Trim();
		}

		static string Unmask(string s, List<string> literals) =>
			Regex.Replace(s, "\u0001(\\d+)\u0002", m =>
			{
				var i = int.Parse(m.Groups[1].Value);
				return i < literals.Count ? literals[i] : m.Value;
			});

		static string Unquote(string s)
		{
			s = s.Trim();
			if (s.StartsWith("N'", StringComparison.Ordinal)) s = s.Substring(1);
			if (s.Length >= 2 && s[0] == '\'' && s[s.Length - 1] == '\'')
				return s.Substring(1, s.Length - 2).Replace("''", "'");
			return s;
		}

		// ── shared helpers ──
		static void AddNested(List<Node> kids, string path, string value, int depth)
		{
			var node = new Node { Kind = "nested", Label = $"{path} ({value.Length:n0} chars)" };
			node.Kids.AddRange(ScanParts(value, depth));
			if (node.Kids.Any(k => k.Structured)) { kids.Add(node); _nodes++; }
		}

		// cheap sniff: is this string worth re-scanning as a payload?
		static bool LooksEncoded(string s)
		{
			var t = s.TrimStart();
			if (t.Length == 0) return false;
			if (t[0] == '{' || t[0] == '[' || t[0] == '<') return true;
			if (t.IndexOf("SQL:", StringComparison.Ordinal) >= 0) return true;
			if (SqlHeadRx.IsMatch(t)) return true;
			if (PctRx.Matches(s).Count >= 3) return true;
			var b64 = Base64Rx.Match(s);
			return b64.Success && b64.Length > s.Trim().Length * 0.8;
		}
	}

	// ─────────────────────────────── html rendering ─────────────────────────────

	public static class Renderer
	{
		public const string PreStyle =
			"white-space:pre-wrap;word-break:break-word;font-family:Consolas,Menlo,monospace;" +
			"font-size:13px;line-height:1.4;margin:3px 0;padding:5px 7px;background:#fbfbfb;" +
			"color:#1a1a1a;border:1px solid #e3e3e3;border-radius:3px;max-height:480px;overflow:auto";

		const int MaxTextChars = 100_000;      // per block
		const int MaxHtmlChars = 1_500_000;    // per message, keeps the output pane responsive

		static readonly Dictionary<string, string> Colours = new Dictionary<string, string>
		{
			["entry"] = "#37474f", ["decoded"] = "#37474f", ["fields"] = "#78909c", ["trace"] = "#6a1b9a",
			["json"] = "#1565c0", ["xml"] = "#ef6c00", ["sql"] = "#2e7d32",
			["sqlparam"] = "#00796b", ["base64"] = "#6d4c41", ["binary"] = "#5d4037",
			["urlenc"] = "#ad1457", ["nested"] = "#455a64", ["image"] = "#0277bd",
		};

		static readonly HashSet<string> AlwaysCollapsed =
			new HashSet<string>(StringComparer.Ordinal) { "fields" };

		const string MarkStyle = "background:#ff5f1f;color:#000;font-weight:bold;border-radius:2px;padding:0 1px";

		// one render: the highlighter and the per-node hit counts it needs, so
		// Write and Pre don't have to carry them as parameters
		sealed class Pass
		{
			public readonly StringBuilder Sb = new StringBuilder();
			public Regex Marker;
			public Dictionary<Node, int> Hits;
		}

		public static string Render(Node n) => Render(n, null);

		// `marker` paints every match neon orange; null renders plainly
		public static string Render(Node n, Regex marker)
		{
			var pass = new Pass { Marker = marker };
			if (marker != null)
			{
				pass.Hits = new Dictionary<Node, int>(ReferenceEqualityComparer.Instance);
				CountHits(pass, n);
			}
			Write(pass, n, 0, 0);
			var sb = pass.Sb;
			if (sb.Length >= MaxHtmlChars)
				sb.Append("<div style='color:#b71c1c;font:11px Verdana'>output truncated - this message is unusually large</div>");
			return sb.ToString();
		}

		// hits per subtree, counted up front because a branch's summary line is
		// written before its children are - and the summary is where a collapsed
		// branch has to admit it is hiding matches
		static int CountHits(Pass pass, Node n)
		{
			var count = Matches(pass.Marker, n.Label) + Matches(pass.Marker, Clip(n.Text));
			foreach (var kid in n.Kids) count += CountHits(pass, kid);
			pass.Hits[n] = count;
			return count;
		}

		static int Matches(Regex marker, string text)
		{
			if (string.IsNullOrEmpty(text)) return 0;
			int count = 0;
			for (var m = marker.Match(text); m.Success; m = m.NextMatch())
				if (m.Length > 0) count++;
			return count;
		}

		// html-encode, wrapping each match in a mark; zero-length matches from a
		// regex like "a*" would otherwise paint nothing between every character
		static string Encode(Pass pass, string text)
		{
			if (string.IsNullOrEmpty(text)) return "";
			if (pass.Marker == null) return System.Net.WebUtility.HtmlEncode(text);

			var sb = new StringBuilder(text.Length + 64);
			int at = 0;
			for (var m = pass.Marker.Match(text); m.Success; m = m.NextMatch())
			{
				if (m.Length == 0) continue;
				sb.Append(System.Net.WebUtility.HtmlEncode(text.Substring(at, m.Index - at)))
				  .Append($"<mark style='{MarkStyle}'>")
				  .Append(System.Net.WebUtility.HtmlEncode(m.Value))
				  .Append("</mark>");
				at = m.Index + m.Length;
			}
			return sb.Append(System.Net.WebUtility.HtmlEncode(text.Substring(at))).ToString();
		}

		static void Write(Pass pass, Node n, int depth, int index)
		{
			var sb = pass.Sb;
			if (sb.Length >= MaxHtmlChars) return;
			if (n.Kind == "text" && n.Kids.Count == 0) { Pre(pass, n.Text); return; }

			if (n.Kind == "image")
			{
				sb.Append($"<div style='margin:3px 0'>{Badge(n)} ")
				  .Append($"<img src='{System.Net.WebUtility.HtmlEncode(n.Text)}' style='max-width:600px;max-height:400px;display:block;margin-top:4px;border:1px solid #ddd'/></div>");
				return;
			}

			// expand the first few branches only; a message with 200 payloads should
			// not arrive fully expanded, and the boilerplate columns are rarely
			// what you opened the decode for
			var open = depth < 3 && index < 8 && !AlwaysCollapsed.Contains(n.Kind) ? " open" : "";
			sb.Append($"<details{open} style='margin:3px 0'><summary style='cursor:pointer;user-select:none'>")
			  .Append(Badge(n));
			if (pass.Hits != null && pass.Hits.TryGetValue(n, out var hits) && hits > 0)
				sb.Append($" <span style='{MarkStyle};font:bold 10px Verdana,sans-serif;padding:1px 5px;border-radius:3px'>" +
						  $"{hits:n0} {(hits == 1 ? "match" : "matches")}</span>");
			if (!string.IsNullOrEmpty(n.Label))
				sb.Append($" <span style='font-family:Consolas,Menlo,monospace;font-size:12px;color:#607d8b'>{Encode(pass, n.Label)}</span>");
			sb.Append("</summary>")
			  .Append("<div style='margin:2px 0 2px 10px;padding-left:8px;border-left:2px solid #e0e0e0'>");

			if (!string.IsNullOrEmpty(n.Text)) Pre(pass, n.Text);
			for (int i = 0; i < n.Kids.Count; i++) Write(pass, n.Kids[i], depth + 1, i);

			sb.Append("</div></details>");
		}

		static void Pre(Pass pass, string text)
		{
			var sb = pass.Sb;
			if (string.IsNullOrEmpty(text) || sb.Length >= MaxHtmlChars) return;
			var more = text.Length > MaxTextChars ? $"\n… {text.Length - MaxTextChars:n0} more characters" : "";
			sb.Append($"<pre style='{PreStyle}'>{Encode(pass, Clip(text))}{System.Net.WebUtility.HtmlEncode(more)}</pre>");
		}

		static string Clip(string text) =>
			text == null || text.Length <= MaxTextChars ? text : text.Substring(0, MaxTextChars);

		static string Badge(Node n)
		{
			var colour = Colours.TryGetValue(n.Kind, out var c) ? c : "#9e9e9e";
			return $"<span style='background:{colour};color:#fff;font:bold 10px Verdana,sans-serif;" +
				   $"padding:1px 5px;border-radius:3px;letter-spacing:.5px'>{n.Kind.ToUpperInvariant()}</span>";
		}
	}

	static string Trunc(string s, int max) =>
		s == null ? null : s.Length <= max ? s : s.Substring(0, max) + "…";

	static string OneLine(string s) =>
		s == null ? null : Regex.Replace(s, @"\s*[\r\n]+\s*", " ⏎ ");

	static string Bytes(long n) =>
		n < 1024 ? $"{n} B" :
		n < 1024 * 1024 ? $"{n / 1024.0:n1} KB" :
		$"{n / (1024.0 * 1024):n1} MB";
}
#endif

public static class Http
{
	// -------------------------------------------------------------------------
	// Shared client factory — callers can swap this out in a query if needed.
	// -------------------------------------------------------------------------
	private static readonly HttpClient _default = new HttpClient();
	public static HttpClient Client { get; set; } = _default;

	// -------------------------------------------------------------------------
	// Core request builder
	// -------------------------------------------------------------------------

	/// <summary>
	/// Fluent entry point. Returns a request builder for the given URL.
	/// Usage: Http.To("https://api.example.com/v1/users").Get()
	/// </summary>
	public static HttpRequest To(string url) => new HttpRequest(url);

	// -------------------------------------------------------------------------
	// Convenience one-liners for the most common patterns
	// -------------------------------------------------------------------------

	public static Task<HttpResult> GetAsync(string url, Action<HttpRequest> configure = null)
		=> BuildAndSend(url, HttpMethod.Get, null, configure);

	public static Task<HttpResult> PostAsync(string url, object body = null, Action<HttpRequest> configure = null)
		=> BuildAndSend(url, HttpMethod.Post, body, configure);

	public static Task<HttpResult> PutAsync(string url, object body = null, Action<HttpRequest> configure = null)
		=> BuildAndSend(url, HttpMethod.Put, body, configure);

	public static Task<HttpResult> PatchAsync(string url, object body = null, Action<HttpRequest> configure = null)
		=> BuildAndSend(url, HttpMethod.Patch, body, configure);

	public static Task<HttpResult> DeleteAsync(string url, Action<HttpRequest> configure = null)
		=> BuildAndSend(url, HttpMethod.Delete, null, configure);

	// -------------------------------------------------------------------------
	// Internal dispatch
	// -------------------------------------------------------------------------

	private static Task<HttpResult> BuildAndSend(string url, HttpMethod method, object body, Action<HttpRequest> configure)
	{
		var req = new HttpRequest(url);
		configure?.Invoke(req);
		if (body != null) req.WithJsonBody(body);
		return req.SendAsync(method);
	}
}

// =============================================================================
// Fluent request builder
// =============================================================================

public class HttpRequest
{
	public string BaseUrl => _baseUrl;
	public List<string> PathSegments => _pathSegments; 
	public Dictionary<string, string> Headers => _headers;
	public List<(string Key, string Value)> QueryParams => _queryParams;
	public HttpContent Body => _body;
	public string BearerToken => _bearerToken;
	public string BasicUser => _basicUser;
	public string BasicPass => _basicPass == null ? "Unset" : "Set";
	public string FullUrl => BuildUrl();

	private readonly string _baseUrl;
	private readonly List<string> _pathSegments = new();
	private readonly Dictionary<string, string> _headers = new();
	private readonly List<(string Key, string Value)> _queryParams = new();
	private HttpContent _body;
	private string _bearerToken;
	private string _basicUser;
	private string _basicPass;

	public HttpRequest(string url) => _baseUrl = url.TrimEnd('/');

	// ---- Path segments ------------------------------------------------------

	/// <summary>Append a path segment. The value is URL-encoded; slashes are treated as separators.</summary>
	public HttpRequest WithPath(string segment)
	{
		foreach (var part in segment.Split('/', StringSplitOptions.RemoveEmptyEntries))
			_pathSegments.Add(Uri.EscapeDataString(part));
		return this;
	}

	/// <summary>Convenience overload — converts value via ToString() (handy for IDs).</summary>
	public HttpRequest WithPath(object value) => WithPath(value?.ToString() ?? "");

	/// <summary>Append multiple segments in one call.</summary>
	public HttpRequest WithPath(params string[] segments)
	{
		foreach (var s in segments) WithPath(s);
		return this;
	}

	// ---- Query string -------------------------------------------------------

	/// <summary>Append a single query parameter. Safe to call multiple times for repeated keys.</summary>
	public HttpRequest WithQuery(string key, string value)
	{
		_queryParams.Add((key, value));
		return this;
	}

	/// <summary>Convenience overload — converts value via ToString().</summary>
	public HttpRequest WithQuery(string key, object value)
		=> WithQuery(key, value?.ToString() ?? "");

	/// <summary>Append multiple parameters from any key/value sequence.</summary>
	public HttpRequest WithQuery(IEnumerable<KeyValuePair<string, string>> pairs)
	{
		foreach (var (k, v) in pairs) _queryParams.Add((k, v));
		return this;
	}

	/// <summary>
	/// Append parameters from an anonymous object's properties.
	/// Null-valued properties are skipped; collections emit one entry per item.
	/// <code>
	/// .WithQuery(new { page = 2, size = 50, status = "active" })
	/// </code>
	/// </summary>
	public HttpRequest WithQuery(object anonymousParams)
	{
		if (anonymousParams == null) return this;
		foreach (var prop in anonymousParams.GetType().GetProperties())
		{
			var val = prop.GetValue(anonymousParams);
			if (val == null) continue;

			if (val is System.Collections.IEnumerable enumerable and not string)
				foreach (var item in enumerable)
					_queryParams.Add((prop.Name, item?.ToString() ?? ""));
			else
				_queryParams.Add((prop.Name, val.ToString()));
		}
		return this;
	}

	// ---- Headers ------------------------------------------------------------

	public HttpRequest WithHeader(string name, string value)
	{
		_headers[name] = value;
		return this;
	}

	public HttpRequest WithHeaders(IEnumerable<KeyValuePair<string, string>> headers)
	{
		foreach (var (k, v) in headers) _headers[k] = v;
		return this;
	}

	public HttpRequest WithBearerToken(string token)
	{
		_bearerToken = token;
		return this;
	}

	public HttpRequest WithBasicAuth(string username, string password)
	{
		_basicUser = username;
		_basicPass = password;
		return this;
	}

	public HttpRequest WithContentType(string contentType)
	{
		_headers["Content-Type"] = contentType;
		return this;
	}

	// ---- Bodies -------------------------------------------------------------

	/// <summary>Serialize <paramref name="value"/> as JSON and set as request body.</summary>
	public HttpRequest WithJsonBody(object value)
	{
		_body = JsonContent.Create(value);
		return this;
	}

	/// <summary>Send a raw JSON string as the request body.</summary>
	public HttpRequest WithRawJsonBody(string json)
	{
		_body = new StringContent(json, Encoding.UTF8, "application/json");
		return this;
	}

	/// <summary>Send a form-urlencoded body from a dictionary.</summary>
	public HttpRequest WithFormBody(IEnumerable<KeyValuePair<string, string>> fields)
	{
		_body = new FormUrlEncodedContent(fields);
		return this;
	}

	/// <summary>Send arbitrary text with the given media type (default text/plain).</summary>
	public HttpRequest WithStringBody(string text, string mediaType = "text/plain")
	{
		_body = new StringContent(text, Encoding.UTF8, mediaType);
		return this;
	}

	// ---- Verb shortcuts -----------------------------------------------------

	public Task<HttpResult> GetAsync() => SendAsync(HttpMethod.Get);
	public Task<HttpResult> PostAsync() => SendAsync(HttpMethod.Post);
	public Task<HttpResult> PutAsync() => SendAsync(HttpMethod.Put);
	public Task<HttpResult> PatchAsync() => SendAsync(HttpMethod.Patch);
	public Task<HttpResult> DeleteAsync() => SendAsync(HttpMethod.Delete);
	public Task<HttpResult> HeadAsync() => SendAsync(HttpMethod.Head);

	// ---- Core send ----------------------------------------------------------

	private string BuildUrl()
	{
		var url = _pathSegments.Count > 0
			? _baseUrl + "/" + string.Join("/", _pathSegments)
			: _baseUrl;

		if (_queryParams.Count == 0) return url;

		var encoded = _queryParams.Select(p =>
			Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value ?? ""));

		var separator = url.Contains('?') ? "&" : "?";
		return url + separator + string.Join("&", encoded);
	}

	public async Task<HttpResult> SendAsync(HttpMethod method)
	{
		using var msg = new HttpRequestMessage(method, BuildUrl());

		// Auth
		if (_bearerToken != null)
			msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
		else if (_basicUser != null)
		{
			var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_basicUser}:{_basicPass}"));
			msg.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds);
		}

		// Headers (skip Content-Type — that's set on the content object)
		foreach (var (k, v) in _headers)
			if (!k.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
				msg.Headers.TryAddWithoutValidation(k, v);

		// Body
		if (_body != null) msg.Content = _body;

		try
		{
			var response = await Http.Client.SendAsync(msg);
			return await HttpResult.FromResponseAsync(response);
		}
		catch (Exception ex)
		{
			return HttpResult.FromException(ex);
		}
	}
}

// =============================================================================
// Result wrapper — no exception at call sites
// =============================================================================

public class HttpResult
{
	public bool Success { get; private set; }
	public int? StatusCode { get; private set; }
	public string Body { get; private set; }
	public HttpResponseHeaders Headers { get; private set; }
	public Exception Error { get; private set; }

	// ---- Factories ----------------------------------------------------------

	internal static async Task<HttpResult> FromResponseAsync(HttpResponseMessage r)
	{
		var body = await r.Content.ReadAsStringAsync();
		return new HttpResult
		{
			Success = r.IsSuccessStatusCode,
			StatusCode = (int)r.StatusCode,
			Body = body,
			Headers = r.Headers,
		};
	}

	internal static HttpResult FromException(Exception ex) =>
		new HttpResult { Success = false, Error = ex };

	// ---- Body helpers -------------------------------------------------------

	/// <summary>Deserialize the JSON body. Returns default(T) on failure.</summary>
	public T As<T>()
	{
		try { return JsonSerializer.Deserialize<T>(Body, _json); }
		catch { return default; }
	}

	/// <summary>Deserialize and dump as a LINQPad DumpContainer-friendly object.</summary>
	public JsonElement AsJson()
	{
		try { return JsonSerializer.Deserialize<JsonElement>(Body, _json); }
		catch { return default; }
	}

	// ---- Assertion helpers --------------------------------------------------

	/// <summary>Throws if the response was not successful (mirrors EnsureSuccessStatusCode).</summary>
	public HttpResult EnsureSuccess()
	{
		if (!Success)
		{
			var detail = Error?.Message ?? $"HTTP {StatusCode}\n{Body}";
			throw new InvalidOperationException($"HTTP request failed: {detail}");
		}
		return this;
	}

	// ---- Display ------------------------------------------------------------

	public override string ToString() =>
		Success
			? $"[{StatusCode}] {Truncate(Body, 200)}"
			: $"[ERROR] {Error?.Message ?? $"{StatusCode}"}";

	private static string Truncate(string s, int max) =>
		s?.Length > max ? s[..max] + "…" : s ?? "";

	private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true,
		WriteIndented = true,
	};
}

#region Advanced - How to multi-target

// The NETx symbol is active when a script runs under .NET x or later.
// (LINQPad also recognizes NETx_0_OR_GREATER in case you enjoy typing.)

#if NET10
// Code that requires .NET 10 or later
#endif

#if NET9
// Code that requires .NET 9 or later
#endif

#if NET8
// Code that requires .NET 8 or later
#endif

#if NET7
// Code that requires .NET 7 or later
#endif

#if NET6
// Code that requires .NET 6 or later
#endif

#if NETCORE
// Code that requires .NET Core or later
#else
// Code that runs under .NET Framework (LINQPad 5)
#endif

// The following symbols are also defined:
// WINDOWS              - when running under Microsoft Windows
// OSX, MACOS, UNIX     - when running under macOS
// CMD                  - when running under the command-line (lprun).

#endregion