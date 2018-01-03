using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Cribbage;

namespace LongShotHelpers
{
    public enum AnimationSpeedSetting
    {
        Fast,
        Regular,
        Slow
    }

    public static class ViewBoxExtensions
    {
        public static double GetScaleFactor(this Viewbox viewbox)
        {
            if (viewbox.Child == null ||
                viewbox.Child is FrameworkElement == false)
                return double.NaN;
            var child = (FrameworkElement) viewbox.Child;
            return viewbox.ActualWidth / child.ActualWidth;
        }
    }

    public class AnimationSpeedsClass
    {
        private AnimationSpeedSetting _speed;

        public AnimationSpeedsClass(AnimationSpeedSetting speed)
        {
            _speed = speed;
            SetSpeedValues();
            NoAnimation = 1;
        }

        public double VerySlow { get; set; }
        public double Slow { get; set; }
        public double Medium { get; set; }
        public double Fast { get; set; }
        public double VeryFast { get; set; }

        public double NoAnimation { get; set; }

        public double DefaultFlipSpeed { get; set; }

        public AnimationSpeedSetting AnimationSpeed
        {
            get => _speed;
            set
            {
                _speed = value;
                SetSpeedValues();
            }
        }

        private void SetSpeedValues()
        {
            double normal = 250;
            if (_speed == AnimationSpeedSetting.Fast)
                normal = 50;
            else if (_speed == AnimationSpeedSetting.Slow)
                normal = 500;

            VerySlow = normal * 20;
            Slow = normal * 4;
            Medium = normal * 2;
            Fast = normal * .75;
            VeryFast = normal * .5;

            DefaultFlipSpeed = Fast;
        }
    }


    public static class StaticHelpers
    {
        public const string lineSeperator = "\r\n";
        public const string propertySeperator = ",";
        public const string objectSeperator = "|";
        public const string listSeperator = ";";
        public const char listSeperatorChar = ';';
        public const string kvpSeperator = "=";

        public const string
            kvpSeperatorOneLine =
                "="; // can't use : because that is used to serialize time objects...but now we arne't serializing Time

        public const char kvpSeperatorChar = '=';

        public static Dictionary<string, Color> StringToColorDictionary { get; } = new Dictionary<string, Color>
        {
            {"Blue", Colors.Blue},
            {"Red", Colors.Red},
            {"White", Colors.White},
            {"Yellow", Colors.Yellow},
            {"Green", Colors.Green},
            {"Brown", Colors.Brown},
            {"DarkGray", Colors.DarkGray},
            {"Black", Colors.Black}
        };

        public static Dictionary<Color, string> ColorToStringDictionary { get; } = new Dictionary<Color, string>
        {
            {Colors.Blue, "Blue"},
            {Colors.Red, "Red"},
            {Colors.White, "White"},
            {Colors.Yellow, "Yellow"},
            {Colors.Green, "Green"},
            {Colors.Brown, "Brown"},
            {Colors.DarkGray, "DarkGray"},
            {Colors.Black, "Black"}
        };

        public static Dictionary<string, string> BackgroundToForegroundDictionary { get; } =
            new Dictionary<string, string>
            {
                {"Blue", "White"},
                {"Red", "White"},
                {"White", "Black"},
                {"Yellow", "Black"},
                {"Green", "Black"},
                {"Brown", "White"},
                {"DarkGray", "White"},
                {"Black", "White"}
            };

        public static ObservableCollection<string> AvailableColors { get; } = new ObservableCollection<string>
        {
            "Blue",
            "Red",
            "White",
            "Yellow",
            "Green",
            "Brown",
            "DarkGray",
            "Black"
        };

        public static Dictionary<Color, Color> BackgroundToForegroundColorDictionary { get; } =
            new Dictionary<Color, Color>
            {
                {Colors.Blue, Colors.White},
                {Colors.Red, Colors.White},
                {Colors.Transparent, Colors.Transparent},
                {Colors.White, Colors.Black},
                {Colors.Yellow, Colors.Black},
                {Colors.Green, Colors.Black},
                {Colors.Brown, Colors.White},
                {Colors.DarkGray, Colors.White},
                {Colors.Black, Colors.White}
            };


        public static bool IsInVisualStudioDesignMode => !(Application.Current is App);

        public static async Task<StorageFolder> GetSaveFolder()
        {
            var token = "default";
            StorageFolder folder = null;
            try
            {
                if (StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
                {
                    folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
                    return folder;
                }
            }
            catch
            {
            }

            var content = "After clicking on \"Close\" pick the default location for all your Catan saved state";
            var dlg = new MessageDialog(content, "Catan");

            await dlg.ShowAsync();

            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add("*");

            folder = await picker.PickSingleFolderAsync();
            if (folder != null)
                StorageApplicationPermissions.FutureAccessList.AddOrReplace(token, folder);
            else
                folder = ApplicationData.Current.LocalFolder;


            return folder;
        }

        public static bool ExcludeCommonKeys(KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Shift || e.Key == VirtualKey.Control || e.Key == VirtualKey.Menu ||
                e.Key == VirtualKey.Tab)
            {
                e.Handled = true;
                return true;
            }

            return false;
        }

        public static bool FilterKeys(string strToCheck, Regex regex)
        {
            return regex.IsMatch(strToCheck);
        }

        public static bool IsPositiveOrZero(this double d)
        {
            return d >= 0;
        }

        public static void TraceMessage(this object o, string toWrite, [CallerMemberName] string cmb = "",
            [CallerLineNumber] int cln = 0, [CallerFilePath] string cfp = "")
        {
#if DEBUG
            Debug.WriteLine($"{cfp}({cln}):{toWrite}\t\t[Caller={cmb}]");
#endif
        }

        public static void Assert(this object o, bool assert, string toWrite, [CallerMemberName] string cmb = "",
            [CallerLineNumber] int cln = 0, [CallerFilePath] string cfp = "")
        {
#if DEBUG
            Debug.Assert(assert, $"{cfp}({cln}):{toWrite}\t\t[Caller={cmb}]");
#endif
        }


        public static double SetupFlipAnimation(bool flipToFaceUp, DoubleAnimation back, DoubleAnimation front,
            double animationTimeInMs, double startAfter = 0)
        {
            if (flipToFaceUp)
            {
                back.To = -90;
                front.To = 0;
                front.Duration = TimeSpan.FromMilliseconds(animationTimeInMs);
                back.Duration = TimeSpan.FromMilliseconds(animationTimeInMs);
                back.BeginTime = TimeSpan.FromMilliseconds(startAfter);
                front.BeginTime = TimeSpan.FromMilliseconds(startAfter + animationTimeInMs);
            }
            else
            {
                back.To = 0;
                front.To = 90;
                back.Duration = TimeSpan.FromMilliseconds(animationTimeInMs);
                front.Duration = TimeSpan.FromMilliseconds(animationTimeInMs);
                front.BeginTime = TimeSpan.FromMilliseconds(startAfter);
                back.BeginTime = TimeSpan.FromMilliseconds(startAfter + animationTimeInMs);
            }

            return animationTimeInMs;
        }

        public static void Assert(bool val, string message, [CallerFilePath] string file = "",
            [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var msg = string.Format($"File: {file}, Method: {memberName}, Line Number: {lineNumber}\n\n{message}");
            Debug.Assert(val, msg);
        }

        public static string GetErrorMessage(string sErr, Exception e, [CallerFilePath] string file = "",
            [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            return string.Format($"{sErr}\nFile: {file}\n{memberName}: {lineNumber}\n\n{e}");
        }

        public static bool IsNumber(VirtualKey key)
        {
            if ((int) key >= (int) VirtualKey.Number0 && (int) key <= (int) VirtualKey.Number9)
                return true;

            if ((int) key >= (int) VirtualKey.NumberPad0 && (int) key <= (int) VirtualKey.NumberPad9)
                return true;

            return false;
        }

        public static bool IsOnKeyPad(VirtualKey key)
        {
            if (IsNumber(key))
                return true;

            switch (key)
            {
                case VirtualKey.Divide:
                case VirtualKey.Multiply:
                case VirtualKey.Subtract:
                case VirtualKey.Decimal:
                case VirtualKey.Enter:
                case VirtualKey.Add:
                case (VirtualKey) 187: // '+' 
                case (VirtualKey) 189: // '-'
                case (VirtualKey) 190: // '.'
                case (VirtualKey) 191: // '/'
                    return true;
                default:
                    break;
            }

            return false;
        }

        /// <summary>
        ///     used by KeyDown handlers to filter out invalid rolls and keys that aren't on the NumPad
        /// </summary>
        /// <param name="key"></param>
        /// <param name="chars"></param>
        /// <returns> returns the value to pass in e.Handled </returns>
        public static bool FilterNumpadKeys(VirtualKey key, char[] chars)
        {
            //
            //  filter out everythign not on Keypad (but other keyboard works too)
            if (!IsOnKeyPad(key)) return false;

            if (chars.Length == 0) // first char
                if (key == VirtualKey.Number0 || key == VirtualKey.NumberPad0)
                    return true;
            if (chars.Length == 1)
            {
                if (chars[0] != '1') return true;

                if (key == VirtualKey.Number0 || key == VirtualKey.Number1 || key == VirtualKey.Number2 ||
                    key == VirtualKey.NumberPad0 || key == VirtualKey.NumberPad1 || key == VirtualKey.NumberPad2)
                    return false;


                return true;
            }

            return false;
        }

        public static void AddDeltaToIntProperty<T>(this T t, string propName, int delta)
        {
            var propInfo = t.GetType().GetTypeInfo().GetDeclaredProperty(propName);
            var n = (int) propInfo.GetValue(t, null);
            n += delta;
            propInfo.SetValue(t, n);
        }


        public static void AddRange<T>(this ObservableCollection<T> oc, IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            foreach (var item in collection) oc.Add(item);
        }

        //
        //  this only supports List<> collections.  beware... :P
        //
        public static string SerializeObject<T>(this T t, IEnumerable<string> propNames,
            string kvpSep = kvpSeperatorOneLine, string propSep = propertySeperator)
        {
            var s = "";

            foreach (var prop in propNames)
            {
                var propInfo = t.GetType().GetTypeInfo().GetDeclaredProperty(prop);


                if (propInfo == null)
                {
                    t.TraceMessage($"No property named {prop} in SerializeObject");
                    continue;
                }

                var typeInfo = propInfo.PropertyType.GetTypeInfo();
                var propValue = propInfo.GetValue(t, null);
                if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listInstance = (IList) propValue;
                    s += prop + kvpSep;
                    foreach (var o in listInstance) s += o + listSeperator;
                    s += propSep;
                }
                else
                {
                    s += string.Format($"{prop}{kvpSep}{propValue}{propSep}");
                }
            }

            return s;
        }

        public static Type GetEnumeratedType(this Type type)
        {
            // provided by Array
            var elType = type.GetElementType();
            if (null != elType) return elType;

            // otherwise provided by collection
            var elTypes = type.GetGenericArguments();
            if (elTypes.Length > 0) return elTypes[0];

            // otherwise is not an 'enumerated' type
            return null;
        }

        public static Dictionary<string, object> DictionaryFromType(object atype)
        {
            if (atype == null) return new Dictionary<string, object>();
            var t = atype.GetType();
            var props = t.GetProperties();
            var dict = new Dictionary<string, object>();
            foreach (var prp in props)
            {
                var value = prp.GetValue(atype, new object[] { });
                dict.Add(prp.Name, value);
            }

            return dict;
        }

        public static Task<Point> DragAsync(UIElement control, PointerRoutedEventArgs origE,
            IDragAndDropProgress progress = null)
        {
            var taskCompletionSource = new TaskCompletionSource<Point>();
            var mousePositionWindow = Window.Current.Content;
            var pointMouseDown = origE.GetCurrentPoint(mousePositionWindow).Position;

            PointerEventHandler pointerMovedHandler = null;
            PointerEventHandler pointerReleasedHandler = null;

            pointerMovedHandler = (s, e) =>
            {
                var pt = e.GetCurrentPoint(mousePositionWindow).Position;

                var delta = new Point
                {
                    X = pt.X - pointMouseDown.X,
                    Y = pt.Y - pointMouseDown.Y
                };

                if (!(control.RenderTransform is CompositeTransform compositeTransform))
                {
                    compositeTransform = new CompositeTransform();
                    control.RenderTransform = compositeTransform;
                }

                compositeTransform.TranslateX += delta.X;
                compositeTransform.TranslateY += delta.Y;
                control.RenderTransform = compositeTransform;
                pointMouseDown = pt;
                progress?.Report(pt);
            };

            pointerReleasedHandler = (s, e) =>
            {
                var localControl = (UIElement) s;
                localControl.PointerMoved -= pointerMovedHandler;
                localControl.PointerReleased -= pointerReleasedHandler;
                localControl.ReleasePointerCapture(origE.Pointer);
                var exitPoint = e.GetCurrentPoint(mousePositionWindow).Position;


                taskCompletionSource.SetResult(exitPoint);
            };

            control.CapturePointer(origE.Pointer);
            control.PointerMoved += pointerMovedHandler;
            control.PointerReleased += pointerReleasedHandler;
            return taskCompletionSource.Task;
        }

        public static void DeserializeObject<T>(this T t, string s, string kvpSep = kvpSeperatorOneLine,
            string propSep = propertySeperator)
        {
            var properties = s.Split(propSep.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            KeyValuePair kvp = null;
            foreach (var line in properties)
            {
                kvp = GetKeyValue(line, kvpSep[0]);
                var propInfo = t.GetType().GetTypeInfo().GetDeclaredProperty(kvp.Key);
                if (propInfo == null)
                {
                    t.TraceMessage($"No property named {kvp.Key} in DeserializeObject");
                    continue;
                }

                var typeInfo = propInfo.PropertyType.GetTypeInfo();
                if (typeInfo.Name == "Guid") continue;
                if (typeInfo.IsEnum)
                {
                    propInfo.SetValue(t, Enum.Parse(propInfo.PropertyType, kvp.Value));
                }
                else if (typeInfo.IsPrimitive)
                {
                    propInfo.SetValue(t, Convert.ChangeType(kvp.Value, propInfo.PropertyType));
                }
                else if (propInfo.PropertyType == typeof(TimeSpan))
                {
                    propInfo.SetValue(t, TimeSpan.Parse(kvp.Value));
                }
                else if (propInfo.PropertyType == typeof(string))
                {
                    propInfo.SetValue(t, kvp.Value);
                }
                else if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = typeInfo.GenericTypeArguments[0];
                    var arrayValues =
                        kvp.Value.Split(listSeperator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    var listType = typeof(List<>).MakeGenericType(typeInfo.GenericTypeArguments);
                    var listInstance = (IList) Activator.CreateInstance(listType);

                    var isPrimitive = elementType.GetTypeInfo().IsPrimitive;
                    var isEnum = elementType.GetTypeInfo().IsEnum;
                    foreach (var val in arrayValues)
                        if (isPrimitive)
                        {
                            var o = Convert.ChangeType(val, elementType);
                            listInstance.Add(o);
                        }
                        else if (isEnum)
                        {
                            var e = Enum.Parse(elementType, val);
                            listInstance.Add(e);
                        }
                        else
                        {
                            t.TraceMessage($"Can't deserialize list of type {elementType.GetTypeInfo()}");
                            break;
                        }

                    propInfo.SetValue(t, listInstance);
                }
                else
                {
                    var error = string.Format(
                        $"need to support {propInfo.PropertyType} in the deserilizer to load {kvp.Key} whose value is {kvp.Value}");
                    t.TraceMessage(error);
                    throw new Exception(error);
                }
            }
        }

        public static List<int> GetIntegerList(string s, char sep = listSeperatorChar)
        {
            var strings = s.Split(new[] {sep}, StringSplitOptions.RemoveEmptyEntries);
            var ret = new List<int>();
            foreach (var v in strings)
                if (v != "")
                    ret.Add(Convert.ToInt32(v));

            return ret;
        }

        public static Stack<int> GetStack(string s, string sep = listSeperator)
        {
            var strings = s.Split(sep.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var ret = new Stack<int>();
            for (var i = strings.Length - 1; i >= 0; i--) ret.Push(Convert.ToInt32(strings[i]));

            return ret;
        }

        public static string GetValue(string s, char sep = kvpSeperatorChar)
        {
            var values = s.Split(sep);
            return values[1];
        }

        public static bool GetBoolValue(string s, char sep = kvpSeperatorChar)
        {
            var values = s.Split(sep);
            return Convert.ToBoolean(values[1]);
        }

        public static int GetIntValue(string s, char sep = kvpSeperatorChar)
        {
            var values = s.Split(sep);
            return Convert.ToInt32(values[1]);
        }

        public static string SetValue(string name, object value)
        {
            return string.Format($"{name}={value}{lineSeperator}");
        }

        public static KeyValuePair GetKeyValue(string s, char sep = kvpSeperatorChar)
        {
            var tokens = s.Split(sep);
            var kvp = new KeyValuePair("", "");
            if (tokens.Length == 2) kvp = new KeyValuePair(tokens[0], tokens[1]);

            return kvp;
        }

        /*
               Given an file with the following form:
               [section1]
               key1=value
               key2=value
               key3=value
               [section2]
               key1=value
               key2=value

           

           Users: dict["section"] = "key1=value\nkey2=value\nkey3=value\n"
       */

        public static Dictionary<string, string> GetSections(string file)
        {
            char[] sep1 = {'['};
            char[] sep2 = {']'};

            var tokens = file.Split(sep1, StringSplitOptions.RemoveEmptyEntries);
            var sections = new Dictionary<string, string>();
            foreach (var s in tokens)
            {
                var tok1 = s.Split(sep2, StringSplitOptions.RemoveEmptyEntries);
                sections.Add(tok1[0], tok1[1]);
            }

            return sections;
        }

        public static async Task<Dictionary<string, string>> LoadSectionsFromFile(StorageFolder folder, string filename)
        {
            var file = await folder.GetFileAsync(filename);
            var contents = await FileIO.ReadTextAsync(file);
            contents = contents.Replace('\r', '\n');
            Dictionary<string, string> sectionsDict = null;
            try
            {
                sectionsDict = GetSections(contents);
            }
            catch (Exception e)
            {
                var content =
                    string.Format(
                        $"Error parsing file {filename}.\nIn File: {folder.Path}\n\nSuggest deleting it.\n\nError parsing sections.\nException info: {e}");
                var dlg = new MessageDialog(content);
                await dlg.ShowAsync();
            }

            return sectionsDict;
        }


        /*
                Given an file with the following form:
                [section1]
                key1=value
                key2=value
                key3=value
                [section2]
                key1=value
                key2=value

            Then parse the file into a Dictionary<string,string> and return it.  

            Users: dict["section"]["key2"] is "value"
        */

        public static async Task<Dictionary<string, Dictionary<string, string>>> LoadSettingsFile(StorageFolder folder,
            string filename)
        {
            var file = await folder.GetFileAsync(filename);
            var contents = await FileIO.ReadTextAsync(file);
            contents = contents.Replace('\r', '\n');
            return await LoadSettingsFile(contents, filename);
        }

        public static async Task<Dictionary<string, Dictionary<string, string>>> LoadSettingsFile(string contents,
            string filename)
        {
            var currentKvp = new KeyValuePair<string, string>();
            var returnDictionary = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> sectionsDict = null;
            try
            {
                sectionsDict = GetSections(contents);
            }
            catch (Exception e)
            {
                var content =
                    string.Format(
                        $"Error parsing file {filename}.\n\nSuggest deleting it.\n\nError parsing sections.\nException info: {e}");
                var dlg = new MessageDialog(content);
                await dlg.ShowAsync();
                return returnDictionary;
            }

            if (sectionsDict.Count == 0)
            {
                var content =
                    string.Format(
                        $"There appears to be no sections in {filename}.\n\nSuggest deleting it.\n\nError parsing sections.");
                var dlg = new MessageDialog(content);
                await dlg.ShowAsync();
                return returnDictionary;
            }

            try
            {
                foreach (var kvp in sectionsDict)
                {
                    currentKvp = kvp;
                    var dict = DeserializeDictionary(kvp.Value);
                    returnDictionary[kvp.Key] = dict;
                }
            }
            catch
            {
                var content =
                    string.Format(
                        $"Error parsing values {filename}.\nSuggest deleting it.\n\nError in section '{currentKvp.Key}' and value '{currentKvp.Value}'");
                var dlg = new MessageDialog(content);
                await dlg.ShowAsync();
            }


            return returnDictionary;
        }

        public static async Task ShowErrorText(string s, string title = "")
        {
            var dlg = new MessageDialog(s, title);
            await dlg.ShowAsync();
        }

        public static string SerializeDictionary(Dictionary<string, string> dictionary,
            string seperator = lineSeperator)
        {
            var ret = "";

            foreach (var kvp in dictionary) ret += string.Format("{0}={1}{2}", kvp.Key, kvp.Value, seperator);

            return ret;
        }

        /*  creates something thant looks like

            Log-1=<>
            Log-2=<>

        */

        public static string SerilizeListToSection<T>(this IList<T> list, string prefix)
        {
            var s = "";
            var n = 0;
            if (list != null)
            {
                var methodInfo = typeof(T).GetTypeInfo().GetDeclaredMethod("Serialize");
                foreach (var item in list)
                {
                    n++;
                    var propValue = methodInfo.Invoke(item, null).ToString();
                    s += string.Format($"{prefix}-{n}{kvpSeperator}{propValue}{lineSeperator}");
                }
            }


            return s;
        }


        /*
         * 
         *  Creates something that looks like AceOfSpaces.Computer, AceOfClubs.Computer, ... 
         * 
         */
        public static string SerilizeListToOneLine<T>(this IList<T> list, string sep = ",",
            string methodName = "Serialize")
        {
            var sb = new StringBuilder();
            if (list != null)
            {
                var methodInfo = typeof(T).GetTypeInfo().GetDeclaredMethod(methodName);
                foreach (var item in list)
                {
                    var propValue = methodInfo.Invoke(item, null).ToString();
                    sb.Append(propValue);
                    sb.Append(sep);
                }
            }

            sb.Remove(sb.Length - sep.Length, sep.Length);

            return sb.ToString();
        }

        public static string SerializeList<T>(this IList<T> list, string sep = listSeperator)
        {
            var s = "";
            if (list != null)
                foreach (var item in list)
                    s += item + sep;

            return s;
        }

        /// <summary>
        ///     This will serialize a IList<> into a string that can be deserialized. You can pass in an arbitrary list of thingies
        ///     and it will serialize the property passed in
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="propName"></param>
        /// <param name="sep"></param>
        /// <returns></returns>
        public static string SerializeListWithProperty<T>(this IList<T> list, string propName,
            string sep = listSeperator)
        {
            var s = "";
            if (list != null)
            {
                var propInfo = typeof(T).GetTypeInfo().GetDeclaredProperty(propName);
                foreach (var item in list)
                {
                    var propValue = propInfo.GetValue(item, null).ToString();
                    s += propValue + sep;
                }
            }

            return s;
        }

        public static bool TryParse<T>(this Enum theEnum, string valueToParse, out T returnValue)
        {
            returnValue = default(T);
            if (int.TryParse(valueToParse, out var intEnumValue))
                if (Enum.IsDefined(typeof(T), intEnumValue))
                {
                    returnValue = (T) (object) intEnumValue;
                    return true;
                }

            return false;
        }

        public static T ParseEnum<T>(string value)
        {
            return (T) Enum.Parse(typeof(T), value);
        }

        public static List<T> DeserializeList<T>(this string s, string sep = listSeperator)
        {
            var list = new List<T>();
            var charSep = sep.ToCharArray();
            var tokens = s.Split(charSep, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tokens)
            {
                var value = (T) Convert.ChangeType(t, typeof(T));
                list.Add(value);
            }

            return list;
        }


        public static List<T> DeserializeEnumList<T>(this string s, string sep = listSeperator)
        {
            var list = new List<T>();
            var charSep = sep.ToCharArray();
            var tokens = s.Split(charSep, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tokens)
            {
                var value = default(T);
                if (Enum.IsDefined(typeof(T), t))
                {
                    value = (T) Enum.Parse(typeof(T), t);
                    list.Add(value);
                }
            }

            return list;
        }


        public static Dictionary<string, string> DeserializeDictionary(string section,
            string lineSep = lineSeperator)
        {
            var dictionary = new Dictionary<string, string>();
            var sep1 = lineSep.ToCharArray();
            char[] sep2 = {'='};

            var tokens = section.Split(sep1, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in tokens)
            {
                var pairs = s.Split(sep2, StringSplitOptions.RemoveEmptyEntries);
                if (pairs.Length == 2)
                    dictionary.Add(pairs[0], pairs[1]);
                else if (pairs.Length == 1)
                    dictionary.Add(pairs[0], "");
                else
                    Debug.Assert(false,
                        string.Format($"Bad token count in DeserializeDictionary. Pairs.Count: {pairs.Length} "));
            }


            return dictionary;
        }

        public static Dictionary<string, string> GetSection(string file, string section)
        {
            var sections = GetSections(file);
            if (sections == null)
                return null;

            return DeserializeDictionary(sections[section]);
        }

        public static void RunStoryBoardAsync(Storyboard sb, double ms = 500, bool setTimeout = true)
        {
            if (setTimeout)
                foreach (var animations in sb.Children)
                    animations.Duration = new Duration(TimeSpan.FromMilliseconds(ms));

            sb.Begin();
        }

        public static async Task RunStoryBoard(Storyboard sb, bool callStop = true, double ms = 500,
            bool setTimeout = true)
        {
            if (setTimeout)
                foreach (var animations in sb.Children)
                    animations.Duration = new Duration(TimeSpan.FromMilliseconds(ms));

            var tcs = new TaskCompletionSource<object>();

            void completed(object s, object e)
            {
                tcs.TrySetResult(null);
            }

            try
            {
                sb.Completed += completed;
                sb.Begin();
                await tcs.Task;
            }
            finally
            {
                sb.Completed -= completed;
                if (callStop) sb.Stop();
            }
        }

        public static List<T> DestructiveIterator<T>(this List<T> list)
        {
            var copy = new List<T>(list);
            return copy;
        }

        public static T Pop<T>(this List<T> list)
        {
            var t = list.Last();
            list.RemoveAt(list.Count - 1);
            return t;
        }

        public static T Pop<T>(this ObservableCollection<T> list)
        {
            var t = list.Last();
            list.RemoveAt(list.Count - 1);
            return t;
        }

        public static void Push<T>(this ObservableCollection<T> list, T t)
        {
            list.Add(t);
        }


        public static void Push<T>(this List<T> list, T t)
        {
            list.Add(t);
        }

        public static T Peek<T>(this List<T> list)
        {
            if (list.Count > 0)
                return list.Last();

            return default(T);
        }

        public static Task BeginAsync(this Storyboard storyboard)
        {
            var tcs = new TaskCompletionSource<bool>();
            if (storyboard == null)
            {
                tcs.SetException(new ArgumentNullException());
            }
            else
            {
                void onComplete(object s, object e)
                {
                    storyboard.Completed -= onComplete;
                    tcs.SetResult(true);
                }

                storyboard.Completed += onComplete;
                storyboard.Begin();
            }

            return tcs.Task;
        }

        public static Task ToTask(this Storyboard storyboard)
        {
            return storyboard.BeginAsync();
        }

        //public static Task<object> ToTask(this Storyboard storyboard, CancellationTokenSource cancellationTokenSource = null)
        //{
        //    TaskCompletionSource<object> tcs = new TaskCompletionSource<object>(TaskCreationOptions.AttachedToParent);

        //    if (cancellationTokenSource != null)
        //    {
        //        // when the task is cancelled, 
        //        // Stop the storyboard
        //        cancellationTokenSource.Token.Register
        //        (
        //            () =>
        //            {
        //                storyboard.Stop();
        //            }
        //        );
        //    }

        //    void onCompleted(object s, object e)
        //    {
        //        storyboard.Completed -= onCompleted;

        //        tcs.SetResult(null);
        //    }

        //    storyboard.Completed += onCompleted;

        //    // start the storyboard during the conversion.
        //    storyboard.Begin();

        //    return tcs.Task;
        //}


        public static void SetFlipAnimationSpeed(Storyboard sb, double milliseconds)
        {
            foreach (var animation in sb.Children)
            {
                if (animation.Duration != TimeSpan.FromMilliseconds(0))
                    animation.Duration = TimeSpan.FromMilliseconds(milliseconds);

                if (animation.BeginTime != TimeSpan.FromMilliseconds(0))
                    animation.BeginTime = TimeSpan.FromMilliseconds(milliseconds);
            }
        }

        public static async Task<bool> AskUserYesNoQuestion(string title, string question, string button1,
            string button2)
        {
            var saidYes = false;


            var dlg = new ContentDialog
            {
                Title = title,
                Content = "\n" + question,
                PrimaryButtonText = button1,
                SecondaryButtonText = button2
            };

            dlg.PrimaryButtonClick += (o, i) => { saidYes = true; };


            await dlg.ShowAsync();


            return saidYes;
        }

        public class KeyValuePair
        {
            public KeyValuePair(string key, string value)
            {
                Key = key;
                Value = value;
            }

            public string Key { get; set; }
            public string Value { get; set; }
        }

        //
        //  an interface called by the drag and drop code so we can simlulate the DragOver behavior
        public interface IDragAndDropProgress
        {
            void Report(Point value);
            void PointerUp(Point value);
        }
    }

    public static class StorageHelper
    {
        public enum StorageStrategies
        {
            /// <summary>Local, isolated folder</summary>
            Local,

            /// <summary>Cloud, isolated folder. 100k cumulative limit.</summary>
            Roaming,

            /// <summary>Local, temporary folder (not for settings)</summary>
            Temporary
        }

        /// <summary>Serializes the specified object as a JSON string</summary>
        /// <param name="objectToSerialize">Specified object to serialize</param>
        /// <returns>JSON string of serialzied object</returns>
        public static string Serialize(object objectToSerialize)
        {
            using (var _Stream = new MemoryStream())
            {
                try
                {
                    var _Serializer = new DataContractJsonSerializer(objectToSerialize.GetType());
                    _Serializer.WriteObject(_Stream, objectToSerialize);
                    _Stream.Position = 0;
                    var _Reader = new StreamReader(_Stream);
                    return _Reader.ReadToEnd();
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>Deserializes the JSON string as a specified object</summary>
        /// <typeparam name="T">Specified type of target object</typeparam>
        /// <param name="jsonString">JSON string source</param>
        /// <returns>Object of specied type</returns>
        public static T Deserialize<T>(string jsonString)
        {
            using (var _Stream = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                try
                {
                    var _Serializer = new DataContractJsonSerializer(typeof(T));
                    return (T) _Serializer.ReadObject(_Stream);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public static async Task DeleteFileFireAndForget(string key, StorageStrategies location)
        {
            await DeleteFileAsync(key, location);
        }

        public static async Task WriteFileFireAndForget<T>(string key, T value, StorageStrategies location)
        {
            await WriteFileAsync(key, value, location);
        }

        // Usage: await button1.WhenClicked();

        public static async Task WhenClicked(this Button button)
        {
            var tcs = new TaskCompletionSource<object>();

            void lambda(object s, RoutedEventArgs e)
            {
                tcs.TrySetResult(null);
            }

            try
            {
                button.Click += lambda;
                await tcs.Task;
            }
            finally
            {
                button.Click -= lambda;
            }
        }

        #region Settings

        /// <summary>Returns if a setting is found in the specified storage strategy</summary>
        /// <param name="key">Path of the setting in storage</param>
        /// <param name="location">Location storage strategy</param>
        /// <returns>Boolean: true if found, false if not found</returns>
        public static bool SettingExists(string key, StorageStrategies location = StorageStrategies.Local)
        {
            switch (location)
            {
                case StorageStrategies.Local:
                    return ApplicationData.Current.LocalSettings.Values.ContainsKey(key);
                case StorageStrategies.Roaming:
                    return ApplicationData.Current.RoamingSettings.Values.ContainsKey(key);
                default:
                    throw new NotSupportedException(location.ToString());
            }
        }

        /// <summary>Reads and converts a setting into specified type T</summary>
        /// <typeparam name="T">Specified type into which to value is converted</typeparam>
        /// <param name="key">Path to the file in storage</param>
        /// <param name="otherwise">Return value if key is not found or convert fails</param>
        /// <param name="location">Location storage strategy</param>
        /// <returns>Specified type T</returns>
        public static T GetSetting<T>(string key, T otherwise = default(T),
            StorageStrategies location = StorageStrategies.Local)
        {
            try
            {
                if (!SettingExists(key, location))
                    return otherwise;
                switch (location)
                {
                    case StorageStrategies.Local:
                        return (T) ApplicationData.Current.LocalSettings.Values[key];
                    case StorageStrategies.Roaming:
                        return (T) ApplicationData.Current.RoamingSettings.Values[key];
                    default:
                        throw new NotSupportedException(location.ToString());
                }
            }
            catch
            {
                /* error casting */
                return otherwise;
            }
        }

        /// <summary>Serializes an object and write to file in specified storage strategy</summary>
        /// <typeparam name="T">Specified type of object to serialize</typeparam>
        /// <param name="key">Path to the file in storage</param>
        /// <param name="value">Instance of object to be serialized and written</param>
        /// <param name="location">Location storage strategy</param>
        public static void SetSetting<T>(string key, T value, StorageStrategies location = StorageStrategies.Local)
        {
            switch (location)
            {
                case StorageStrategies.Local:
                    ApplicationData.Current.LocalSettings.Values[key] = value;
                    break;
                case StorageStrategies.Roaming:
                    ApplicationData.Current.RoamingSettings.Values[key] = value;
                    break;
                default:
                    throw new NotSupportedException(location.ToString());
            }
        }

        public static void DeleteSetting(string key, StorageStrategies location = StorageStrategies.Local)
        {
            switch (location)
            {
                case StorageStrategies.Local:
                    ApplicationData.Current.LocalSettings.Values.Remove(key);
                    break;
                case StorageStrategies.Roaming:
                    ApplicationData.Current.RoamingSettings.Values.Remove(key);
                    break;
                default:
                    throw new NotSupportedException(location.ToString());
            }
        }

        #endregion

        #region File

        /// <summary>Returns if a file is found in the specified storage strategy</summary>
        /// <param name="key">Path of the file in storage</param>
        /// <param name="location">Location storage strategy</param>
        /// <returns>Boolean: true if found, false if not found</returns>
        public static async Task<bool> FileExistsAsync(string key, StorageStrategies location = StorageStrategies.Local)
        {
            return await GetIfFileExistsAsync(key, location) != null;
        }

        public static async Task<bool> FileExistsAsync(string key, StorageFolder folder)
        {
            return await GetIfFileExistsAsync(key, folder) != null;
        }

        /// <summary>Deletes a file in the specified storage strategy</summary>
        /// <param name="key">Path of the file in storage</param>
        /// <param name="location">Location storage strategy</param>
        public static async Task<bool> DeleteFileAsync(string key, StorageStrategies location = StorageStrategies.Local)
        {
            var _File = await GetIfFileExistsAsync(key, location);
            if (_File != null)
                await _File.DeleteAsync();
            return !await FileExistsAsync(key, location);
        }

        /// <summary>Reads and deserializes a file into specified type T</summary>
        /// <typeparam name="T">Specified type into which to deserialize file content</typeparam>
        /// <param name="key">Path to the file in storage</param>
        /// <param name="location">Location storage strategy</param>
        /// <returns>Specified type T</returns>
        public static async Task<T> ReadFileAsync<T>(string key, StorageStrategies location = StorageStrategies.Local)
        {
            try
            {
                // fetch file
                var _File = await GetIfFileExistsAsync(key, location);
                if (_File == null)
                    return default(T);
                // read content
                var _String = await FileIO.ReadTextAsync(_File);
                // convert to obj
                var _Result = Deserialize<T>(_String);
                return _Result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>Serializes an object and write to file in specified storage strategy</summary>
        /// <typeparam name="T">Specified type of object to serialize</typeparam>
        /// <param name="key">Path to the file in storage</param>
        /// <param name="value">Instance of object to be serialized and written</param>
        /// <param name="location">Location storage strategy</param>
        public static async Task<bool> WriteFileAsync<T>(string key, T value,
            StorageStrategies location = StorageStrategies.Local)
        {
            // create file
            var _File = await CreateFileAsync(key, location, CreationCollisionOption.ReplaceExisting);
            // convert to string
            var _String = Serialize(value);
            // save string to file
            await FileIO.WriteTextAsync(_File, _String);
            // result
            return await FileExistsAsync(key, location);
        }

        private static async Task<StorageFile> CreateFileAsync(string key,
            StorageStrategies location = StorageStrategies.Local,
            CreationCollisionOption option = CreationCollisionOption.OpenIfExists)
        {
            switch (location)
            {
                case StorageStrategies.Local:
                    return await ApplicationData.Current.LocalFolder.CreateFileAsync(key, option);
                case StorageStrategies.Roaming:
                    return await ApplicationData.Current.RoamingFolder.CreateFileAsync(key, option);
                case StorageStrategies.Temporary:
                    return await ApplicationData.Current.TemporaryFolder.CreateFileAsync(key, option);
                default:
                    throw new NotSupportedException(location.ToString());
            }
        }

        private static async Task<StorageFile> GetIfFileExistsAsync(string key, StorageFolder folder,
            CreationCollisionOption option = CreationCollisionOption.FailIfExists)
        {
            StorageFile retval;
            try
            {
                retval = await folder.GetFileAsync(key);
            }
            catch (FileNotFoundException)
            {
                // System.Diagnostics.Debug.WriteLine("GetIfFileExistsAsync:FileNotFoundException");
                return null;
            }

            return retval;
        }

        /// <summary>Returns a file if it is found in the specified storage strategy</summary>
        /// <param name="key">Path of the file in storage</param>
        /// <param name="location">Location storage strategy</param>
        /// <returns>StorageFile</returns>
        private static async Task<StorageFile> GetIfFileExistsAsync(string key,
            StorageStrategies location = StorageStrategies.Local,
            CreationCollisionOption option = CreationCollisionOption.FailIfExists)
        {
            StorageFile retval;
            try
            {
                switch (location)
                {
                    case StorageStrategies.Local:
                        retval = await ApplicationData.Current.LocalFolder.GetFileAsync(key);
                        break;
                    case StorageStrategies.Roaming:
                        retval = await ApplicationData.Current.RoamingFolder.GetFileAsync(key);
                        break;
                    case StorageStrategies.Temporary:
                        retval = await ApplicationData.Current.TemporaryFolder.GetFileAsync(key);
                        break;
                    default:
                        throw new NotSupportedException(location.ToString());
                }
            }
            catch (FileNotFoundException)
            {
                return null;
            }

            return retval;
        }

        #endregion
    }
}