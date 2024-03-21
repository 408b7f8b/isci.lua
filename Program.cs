using System;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using isci.Allgemein;
using isci.Daten;
using isci.Beschreibung;
using System.Data.Common;

using NLua;
using NLua.Exceptions;

namespace isci.lua
{
    class Program
    {
        static void variablenAusStrukturInLua(Datenstruktur structure, dynamic lua)
        {
            foreach (var DateneintragPair in structure.dateneinträge)
            {
                var Dateneintrag = DateneintragPair.Value;
                /*string type = "";
                string value = "";

                switch(Dateneintrag.type)
                {
                    case Datentypen.UInt8:
                    case Datentypen.UInt16:
                    case Datentypen.UInt32:
                    case Datentypen.Int8:
                    case Datentypen.Int16:
                    case Datentypen.Int32:
                    case Datentypen.Float:
                    case Datentypen.Double:
                        type = "number";
                        value = Dateneintrag.Serialisieren();
                        break;
                    case Datentypen.Bool:
                        type = "boolean"; 
                        value = Dateneintrag.Serialisieren();
                        break;
                    case Datentypen.String:
                        type = "string";
                        value = "’" + Dateneintrag.Serialisieren() + "’";
                        break;
                    default: continue;
                }

                var lua_variable = type + " = " + value;*/

                var lua_variable = DateneintragPair.Key.Replace('.', '_') + " = ";

                switch (Dateneintrag.type)
                {
                    case Datentypen.String: lua_variable += "'" + Dateneintrag.WertSerialisieren() + "'"; break;
                    case Datentypen.UInt8:
                    case Datentypen.UInt16:
                    case Datentypen.UInt32:
                    case Datentypen.UInt64:
                    case Datentypen.Int8:
                    case Datentypen.Int16:
                    case Datentypen.Int32:
                    case Datentypen.Int64:
                    case Datentypen.Float:
                    case Datentypen.Double:
                    case Datentypen.Bool:
                    lua_variable += Dateneintrag.WertSerialisieren();
                    break;
                    default: continue;
                }

                lua.DoString(lua_variable);
            }
        }

        static void LuaVariablenInStruktur(Datenstruktur structure, dynamic lua)
        {
            foreach (var DateneintragPair in structure.dateneinträge)
            {
                var Dateneintrag = DateneintragPair.Value;

                switch (Dateneintrag.type)
                {
                    case Datentypen.String:
                    case Datentypen.UInt8:
                    case Datentypen.UInt16:
                    case Datentypen.UInt32:
                    case Datentypen.UInt64:
                    case Datentypen.Int8:
                    case Datentypen.Int16:
                    case Datentypen.Int32:
                    case Datentypen.Int64:
                    case Datentypen.Float:
                    case Datentypen.Double:
                    case Datentypen.Bool:
                    Dateneintrag.WertAusString(lua[DateneintragPair.Key.Replace('.', '_')].ToString());
                    break;
                }
            }
        }

        static Lua lua;

        static void Main(string[] args)
        {
            var konfiguration = new Parameter(args);

            if (!System.IO.File.Exists("main.lua"))
            {
                Logger.Fatal("Die notwendige Skriptdatei main.lua existiert nicht.");
                System.Environment.Exit(-1);
            }
            
            var structure = new Datenstruktur(konfiguration);
            var ausfuehrungsmodell = new Ausführungsmodell(konfiguration, structure.Zustand);

            lua = new Lua();

            var globalTable = lua.GetTable("_G");
            var luaStandardVariablen = new List<string>();

            var en_name = globalTable.Keys.GetEnumerator();
            for (int i = 0; i < globalTable.Keys.Count; ++i)
            {
                en_name.MoveNext();
                luaStandardVariablen.Add((string)en_name.Current);
                Logger.Information("Schließe Lua-Standardvariable aus: " + luaStandardVariablen.Last());
            }
            
            Logger.Information("Lade Lua-Dateien.");
            string var_mapping_datei = "variables_mapping.json";
            string[] files = {};
            if (System.IO.File.Exists(konfiguration.OrdnerKonfigurationen + "/main.lua"))
            {
                Logger.Information("main.lua liegt im Konfigurationenordner.");
                files = System.IO.Directory.GetFiles(konfiguration.OrdnerKonfigurationen, "*.lua");
                var_mapping_datei = konfiguration.OrdnerKonfigurationen + "/" + var_mapping_datei;
            } else if (System.IO.File.Exists("main.lua")) {
                Logger.Information("main.lua liegt im Ausführungsverzeichnis.");
                files = System.IO.Directory.GetFiles(System.IO.Directory.GetCurrentDirectory(), "*.lua");
            } else {
                Logger.Fatal("Es existiert keine main.lua im Konfigurationenordner oder im Ausführungsordner.");
                System.Environment.Exit(-1);
            }

            foreach (var file in files)
            {
                try {
                    Logger.Information("Lade " + file);
                    lua.LoadFile(file);
                } catch (System.Exception e) {
                    Logger.Fatal("Ausnahme beim Laden der " + file + ": " + e.Message);
                    System.Environment.Exit(-1);
                }
            }

            var variable_mapping = new Dictionary<string, string>();
            if (System.IO.File.Exists(var_mapping_datei))
            {
                try {
                    Logger.Information(var_mapping_datei + " wird für die fixe Bindung von Lua-Variablen an ISCI-Dateneinträge verwendet.");
                    var file = System.IO.File.ReadAllText(var_mapping_datei);
                    variable_mapping = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(file);
                } catch (System.Exception e)
                {
                    Logger.Fatal("Ausnahme beim Öffnen/Verarbeiten der " + var_mapping_datei + ": " + e.Message);
                    System.Environment.Exit(-1);
                }
            }

            var dm = new Datenmodell(konfiguration.Identifikation);

            en_name = globalTable.Keys.GetEnumerator();
            var en = globalTable.Values.GetEnumerator();

            for (int i = 0; i < globalTable.Keys.Count; ++i)
            {
                en_name.MoveNext();
                en.MoveNext();
                var name = (string)en_name.Current;
                if (luaStandardVariablen.Contains(name)) continue;
                if (variable_mapping.ContainsKey(name)) continue;
                if (!name.StartsWith("PUB_")) continue;
                name = name.Substring("PUB_".Length);
                var value = en.Current;
                var typ = value.GetType();
                var logtext = "Variable aus Lua: " + name + "=";
                if (typ == typeof(System.String))
                {
                    Logger.Information(logtext + "'" + (string)value + "'");
                    dm.Add(new dtString((System.String)value, name));
                }
                else if (typ == typeof(System.Int64))
                {
                    Logger.Information(logtext + ((System.Int64)value).ToString());
                    dm.Add(new dtInt64((System.Int64)value, name));
                }
                else if (typ == typeof(double))
                {
                    Logger.Information(logtext + ((double)value).ToString());
                    dm.Add(new dtDouble((double)value, name));
                }
            }

            new Modul(konfiguration.Identifikation, "isci.lua", dm.Dateneinträge) {
                Name = "Lua Ressource " + konfiguration.Identifikation,
                Beschreibung = "Lua"
            }.Speichern(konfiguration);

            dm.Speichern(konfiguration.OrdnerDatenmodelle + "/" + konfiguration.Identifikation + ".json");

            structure.DatenmodellEinhängen(dm);
            structure.DatenmodelleEinhängenAusOrdner(konfiguration.OrdnerDatenmodelle);
            structure.Start();

            variablenAusStrukturInLua(structure, lua);

            while(true)
            {
                structure.Zustand.WertAusSpeicherLesen();

                if (ausfuehrungsmodell.AktuellerZustandModulAktivieren())
                {
                    /* foreach (var dateneintrag in structure.dateneinträge)
                    {
                        if (dateneintrag.Value.type == Datentypen.String)
                        {
                            lua[dateneintrag.Key] = "'" + dateneintrag.Value.WertSerialisieren() + "'";
                        } else {
                            lua[dateneintrag.Key] = dateneintrag.Value.WertSerialisieren();
                        }
                    } */
                    variablenAusStrukturInLua(structure, lua);

                    foreach (var variable in variable_mapping)
                    {
                        var dateneintrag = structure.dateneinträge[variable.Value];

                        lua[variable.Key] = dateneintrag.WertSerialisieren();
                    }

                    lua.DoString("main()"); //Verarbeitung

                    LuaVariablenInStruktur(structure, lua);

                    foreach (var variable in variable_mapping)
                    {
                        var dateneintrag = structure.dateneinträge[variable.Value];

                        switch (dateneintrag.type)
                        {
                            case Datentypen.String:
                            case Datentypen.UInt8:
                            case Datentypen.UInt16:
                            case Datentypen.UInt32:
                            case Datentypen.UInt64:
                            case Datentypen.Int8:
                            case Datentypen.Int16:
                            case Datentypen.Int32:
                            case Datentypen.Int64:
                            case Datentypen.Float:
                            case Datentypen.Double:
                            case Datentypen.Bool:
                            dateneintrag.WertAusString(lua[variable.Key].ToString());
                            break;
                        }
                    }

                    structure.Schreiben();

                    ausfuehrungsmodell.Folgezustand();
                    structure.Zustand.WertInSpeicherSchreiben();
                }

                System.Threading.Thread.Sleep(1);
                //Helfer.SleepForMicroseconds()
            }
        }
    }
}