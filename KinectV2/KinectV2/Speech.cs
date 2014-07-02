using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Text;
using System.Text.RegularExpressions;

namespace KinectV2 {
  class Speech {
    private readonly string GOTO = "goto";
    private readonly string SHOW = "show";
    private readonly string LOCATION = "location";
    private readonly string GEODATA_EARTHQUAKE = "geodata_eq";
    private readonly string GEODATA_TWITTER = "geodata_tw";
    private readonly string HASHTAG = "hashtag";
    private readonly string LAT = "lat";
    private readonly string LNG = "lng";
    private readonly string RADIUS = "radius";
    private readonly string DATE = "date";

    private GlobeModel model;

    public Speech(GlobeModel model) {
      this.model = model;
    }

    private GrammarBuilder gotoGrammar() {
      return new ChainedGrammar("go to", GOTO).append(locations()).build();
    }

    private GrammarBuilder displayGrammar() {
      return new ChainedGrammar("show", SHOW).append(geodataOptions())
          .append(new Choices(new GrammarBuilder[]{
              new ChainedGrammar("within a").appendDictation(RADIUS)
                  .append("radius of").append(specific()).build(),
              new ChainedGrammar("of").append(specific()).build()
          })).build();
    }

    private Choices geodataOptions() {
      return new Choices(new GrammarBuilder[] {
          new ChainedGrammar("earthquake data", GEODATA_EARTHQUAKE).build(),
          new ChainedGrammar("twitter posts with hash tags", GEODATA_TWITTER)
              .appendDictation(HASHTAG).build()
      });
    }
    
    private Choices locations() {
      return new Choices(new GrammarBuilder[] {
          new ChainedGrammar().appendDictation(LOCATION).build(),
          new ChainedGrammar("latitude").appendDictation(LAT).append("longitude").appendDictation(LNG).build()
      });
    }

    private SemanticResultKey specific() {
      return new SemanticResultKey("specific", new ChainedGrammar().append(locations())
          .append(new GrammarBuilder(new ChainedGrammar("from")
              .appendDictation(DATE).build(), 0, 1)).build());
    }

    private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e) {
      SemanticValue result = e.Result.Semantics;
      if (result.ContainsKey(GOTO)) {
        if (result.ContainsKey(LOCATION)) {
          jumpToLocation(new Location(result[LOCATION].Value.ToString()));
        } else {
          jumpToLocation(new Location(result[LAT].Value.ToString().ToLower(), result[LNG].Value.ToString().ToLower()));
        }
      } else if (result.ContainsKey(SHOW)) {
        if (result.ContainsKey(GEODATA_EARTHQUAKE)) {
          Console.WriteLine("geodata: Earthquake");
        } else if (result.ContainsKey(GEODATA_TWITTER)) {
          Console.WriteLine("geodata: Twitter");
          Console.WriteLine("hashtag: " + result[HASHTAG].Value);
        }

        if (result.ContainsKey(RADIUS)) {
          Console.WriteLine("radius: " + result[RADIUS].Value);
        }

        if (result.ContainsKey(DATE)) {
          Console.WriteLine("date: " + result[DATE].Value);
        }

        if (result.ContainsKey(LOCATION)) {
          Console.WriteLine("location: " + result[LOCATION].Value);
        } else {
          Console.WriteLine("location: " + result[LAT].Value + " " + result[LNG].Value);
        }
      }
    }

    private void jumpToLocation(Location location) {
      if (location.success) {
        model.rotateTo(location.latitude, location.longitude);
      }
    }
  }

  

  class ChainedGrammar {
    private GrammarBuilder builder;

    public ChainedGrammar() {
      builder = new GrammarBuilder();
    }

    public ChainedGrammar(string phrase) {
      builder = new GrammarBuilder(phrase);
    }

    public ChainedGrammar(string phrase, string key) {
      builder = new GrammarBuilder();
      builder.Append(new SemanticResultKey(key, new GrammarBuilder(phrase)));
    }

    public ChainedGrammar(Choices choices) {
      builder = new GrammarBuilder(choices);
    }

    public ChainedGrammar append(string phrase) {
      builder.Append(phrase);
      return this;
    }

    public ChainedGrammar append(Choices choices) {
      builder.Append(choices);
      return this;
    }

    public ChainedGrammar append(GrammarBuilder builder) {
      this.builder.Append(builder);
      return this;
    }

    public ChainedGrammar appendDictation(string key) {
      GrammarBuilder b = new GrammarBuilder();
      b.AppendDictation();
      builder.Append(new SemanticResultKey(key, b));
      return this;
    }

    public GrammarBuilder build() {
      return builder;
    }
  }
}
