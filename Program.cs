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

                var lua_variable = DateneintragPair.Key + " = ";
                if (Dateneintrag.type == Datentypen.String)
                {
                    lua_variable += "'" + Dateneintrag.WertSerialisieren() + "'";
                } else {
                    lua_variable += Dateneintrag.WertSerialisieren();
                }

                lua.DoString(lua_variable);
            }
        }

        static void LuaVariablenInStruktur(Datenstruktur structure, dynamic lua)
        {
            foreach (var DateneintragPair in structure.dateneinträge)
            {
                var Dateneintrag = DateneintragPair.Value;

                Dateneintrag.WertAusString(lua[DateneintragPair.Key].ToString());
            }
        }

        static Lua lua;

        static void Main(string[] args)
        {
            /* dynamic lua = new DynamicLua.DynamicLua();
            lua.DoFile("program.lua"); */

            var konfiguration = new Parameter(args);
            
            var structure = new Datenstruktur(konfiguration);
            var ausfuehrungsmodell = new Ausführungsmodell(konfiguration, structure.Zustand);

            var dm = new Datenmodell(konfiguration.Identifikation);

            lua = new Lua();

            lua.LoadFile("program.lua");
            var globalTable = lua.GetTable("_G");

            var en_name = globalTable.Keys.GetEnumerator();
            var en = globalTable.Values.GetEnumerator();

            for (int i = 0; i < globalTable.Keys.Count; ++i)
            {
                en_name.MoveNext();
                en.MoveNext();
                var value = en.Current;
                var name = (string)en_name.Current;
                var typ = value.GetType();
                if (typ == typeof(System.String))
                {
                    if (name == "_VERSION") continue;
                    Console.WriteLine(name);
                    dm.Add(new dtString((System.String)value, name));
                }
                else if (typ == typeof(System.Int64))
                {
                    Console.WriteLine(name);
                    dm.Add(new dtInt64((System.Int64)value, name));
                }
                else if (typ == typeof(double))
                {
                    Console.WriteLine(name);
                    dm.Add(new dtDouble((double)value, name));
                }
            }

            var beschreibung = new Modul(konfiguration.Identifikation, "isci.lua", dm.Dateneinträge);
            beschreibung.Name = "Lua Ressource " + konfiguration.Identifikation;
            beschreibung.Beschreibung = "Lua";
            beschreibung.Speichern(konfiguration.OrdnerBeschreibungen + "/" + konfiguration.Identifikation + ".json");

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
                    foreach (var dateneintrag in structure.dateneinträge)
                    {
                        if (dateneintrag.Value.type == Datentypen.String)
                        {
                            lua[dateneintrag.Key] = "'" + dateneintrag.Value.WertSerialisieren() + "'";
                        } else {
                            lua[dateneintrag.Key] = dateneintrag.Value.WertSerialisieren();
                        }
                    }

                    lua.DoString("main()");

                    foreach (var dateneintrag in structure.dateneinträge)
                    {
                        if (dateneintrag.Value.type == Datentypen.String)
                        {
                            dateneintrag.Value.Wert = (string)lua[dateneintrag.Key];
                        }
                        else if (dateneintrag.Value.type == Datentypen.Int64)
                        {
                            dateneintrag.Value.Wert = (Int64)lua[dateneintrag.Key];
                        }
                        else if (dateneintrag.Value.type == Datentypen.Double)
                        {
                            dateneintrag.Value.Wert = (double)lua[dateneintrag.Key];
                        }
                    }

                    variablenAusStrukturInLua(structure, lua);
/* 
                    lua.DoFile("program.lua"); */

                    structure.Schreiben();

                    ausfuehrungsmodell.Folgezustand();
                    structure.Zustand.WertInSpeicherSchreiben();
                }
            }
        }
    }
}