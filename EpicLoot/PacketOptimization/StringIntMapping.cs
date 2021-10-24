using fastJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicLoot.PacketOptimization {
    public class StringIntMapping {

        public const int UNDEFINED = -1;
        public const int ERROR = -2;

        private Dictionary<int,string> _int2String = new Dictionary<int, string>();
        private Dictionary<string,int> _string2int = new Dictionary<string, int>();

        public StringIntMapping() {
            
        }

        public void LoadFromList(List<string> keys) {
            int id = 1;
            foreach (string key in keys) {
                this._int2String.Add(id, key);
                this._string2int.Add(key, id);
                id++;
            }
        }

        public string GetStringKey(int key) {
            if (key < 0) {
                return null;
            }

            if (this._int2String.ContainsKey(key)) {
                return this._int2String[key];
            }

            return null;
        }

        public int GetIntKey(string key) {
            if (key == null) {
                return UNDEFINED;
            }

            if (this._string2int.ContainsKey(key)) {
                return this._string2int[key];
            }

            return ERROR;
        }

        public string Serialize() {
            return JSON.ToJSON(this._string2int);
        }

        public void Deserialize(string text) {
            
            var mapDef = (IDictionary<string, object>)JSON.Parse(text);
            foreach (var kvp in mapDef) {
                int value = UNDEFINED;
                if (kvp.Value.GetType() == typeof(long)) {
                    value = (int)(long)kvp.Value;
                } else if (kvp.Value.GetType() == typeof(int)) {
                    value = (int)kvp.Value;
                } else if (kvp.Value.GetType() == typeof(string)) {
                    try {
                        value = int.Parse((string)kvp.Value);
                    } catch (Exception) {
                        value = UNDEFINED;
                    }
                }

                if (value != UNDEFINED) {
                    this._int2String.Add(value, kvp.Key);
                    this._string2int.Add(kvp.Key, value);
                } else {
                    EpicLoot.LogError($"Could not parse mapped value for \"{kvp.Key}\"");
                }
            }
        }


    }
}
