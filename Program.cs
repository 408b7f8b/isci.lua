using System;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using isci.Allgemein;
using isci.Daten;
using isci.Beschreibung;

namespace isci.lua
{
    public class Konfiguration : Parameter
    {
        public Konfiguration(string datei) : base(datei) {

        }
    }

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
                    lua_variable += "’" + Dateneintrag.Serialisieren() + "’";
                } else {
                    lua_variable += Dateneintrag.Serialisieren();
                }

                lua(lua_variable);
            }
        }

        static void LuaVariablenInStruktur(Datenstruktur structure, dynamic lua)
        {
            foreach (var DateneintragPair in structure.dateneinträge)
            {
                var Dateneintrag = DateneintragPair.Value;

                Dateneintrag.AusString(lua[DateneintragPair.Key].ToString());
            }
        }

        static void Main(string[] args)
        {
            var konfiguration = new Konfiguration("konfiguration.json");
            
            var structure = new Datenstruktur(konfiguration.OrdnerDatenstruktur);            

            var dm = new Datenmodell(konfiguration.Identifikation);           
            var example = new dtInt32(0, "example");
            dm.Dateneinträge.Add(example);

            var beschreibung = new Modul(konfiguration.Identifikation, "isci.modulbasis", new ListeDateneintraege(){example});
            beschreibung.Name = "Modulbasis Ressource " + konfiguration.Identifikation;
            beschreibung.Beschreibung = "Modulbasis";
            beschreibung.Speichern(konfiguration.OrdnerBeschreibungen + "/" + konfiguration.Identifikation + ".json");

            dm.Speichern(konfiguration.OrdnerDatenmodelle + "/" + konfiguration.Identifikation + ".json");

            structure.DatenmodellEinhängen(dm);
            structure.DatenmodelleEinhängenAusOrdner(konfiguration.OrdnerDatenmodelle);
            structure.Start();

            var Zustand = new dtZustand(konfiguration.OrdnerDatenstruktur);
            Zustand.Start();

            dynamic lua = new DynamicLua.DynamicLua();

            variablenAusStrukturInLua(structure, lua);

            while(true)
            {
                Zustand.Lesen();

                var erfüllteTransitionen = konfiguration.Ausführungstransitionen.Where(a => a.Eingangszustand == (System.Int32)Zustand.value);
                if (erfüllteTransitionen.Count<Ausführungstransition>() <= 0) continue;

                variablenAusStrukturInLua(structure, lua);

                lua.DoFile("program.lua");
                
                structure.Schreiben();

                Zustand.value = erfüllteTransitionen.First<Ausführungstransition>().Ausgangszustand;
                Zustand.Schreiben();
            }
        }
    }
}