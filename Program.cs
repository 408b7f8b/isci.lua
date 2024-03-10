using System;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using isci.Allgemein;
using isci.Daten;
using isci.Beschreibung;
using System.Data.Common;

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
                    lua_variable += "’" + Dateneintrag.WertSerialisieren() + "’";
                } else {
                    lua_variable += Dateneintrag.WertSerialisieren();
                }

                lua(lua_variable);
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

        static void Main(string[] args)
        {
            var konfiguration = new Parameter(args);
            
            var structure = new Datenstruktur(konfiguration);
            var ausfuehrungsmodell = new Ausführungsmodell(konfiguration, structure.Zustand);

            var dm = new Datenmodell(konfiguration.Identifikation);           
            var example = new dtInt32(0, "example");
            dm.Dateneinträge.Add(example);

            var beschreibung = new Modul(konfiguration.Identifikation, "isci.lua", new ListeDateneintraege(){example});
            beschreibung.Name = "Lua Ressource " + konfiguration.Identifikation;
            beschreibung.Beschreibung = "Lua";
            beschreibung.Speichern(konfiguration.OrdnerBeschreibungen + "/" + konfiguration.Identifikation + ".json");

            dm.Speichern(konfiguration.OrdnerDatenmodelle + "/" + konfiguration.Identifikation + ".json");

            structure.DatenmodellEinhängen(dm);
            structure.DatenmodelleEinhängenAusOrdner(konfiguration.OrdnerDatenmodelle);
            structure.Start();

            dynamic lua = new DynamicLua.DynamicLua();

            variablenAusStrukturInLua(structure, lua);

            while(true)
            {
                structure.Zustand.WertAusSpeicherLesen();

                if (ausfuehrungsmodell.AktuellerZustandModulAktivieren())
                {
                    variablenAusStrukturInLua(structure, lua);

                    lua.DoFile("program.lua");

                    structure.Schreiben();

                    ausfuehrungsmodell.Folgezustand();
                    structure.Zustand.WertInSpeicherSchreiben();
                }
            }
        }
    }
}