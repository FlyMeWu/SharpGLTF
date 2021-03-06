﻿using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.IO
{
    class _TestStructure
    {
        public string Author { get; set; }
        public int Integer1 { get; set; }
        public bool Bool1 { get; set; }
        public float Single1 { get; set; }
        public float Single2 { get; set; }
        public float Single3 { get; set; }

        // public float SinglePI { get; set; } // Fails on .Net Framework 471

        public double Double1 { get; set; }
        public double Double2 { get; set; }
        public double Double3 { get; set; }
        public double DoublePI { get; set; }

        public List<int> Array1 { get; set; }

        public _TestStructure2 Dict1 { get; set; }
        public _TestStructure3 Dict2 { get; set; }
    }

    struct _TestStructure2
    {
        public int A0 { get; set; }
        public int A1 { get; set; }
    }

    class _TestStructure3
    {
        public int A { get; set; }
        public string B { get; set; }

        public int[] C { get; set; }

        public _TestStructure2 D { get; set; }
    }

    [Category("Core.IO")]
    public class JsonContentTests
    {
        // when serializing a JsonContent object, it's important to take into account floating point values roundtrips.
        // it seems that prior NetCore3.1, System.Text.JSon was not roundtrip proven, so some values might have some
        // error margin when they complete a roundtrip.

        // On newer, NetCore System.Text.Json versions, it seems to use "G9" and "G17" text formatting are used.

        // https://devblogs.microsoft.com/dotnet/floating-point-parsing-and-formatting-improvements-in-net-core-3-0/            
        // https://github.com/dotnet/runtime/blob/76904319b41a1dd0823daaaaae6e56769ed19ed3/src/libraries/System.Text.Json/src/System/Text/Json/Writer/Utf8JsonWriter.WriteValues.Float.cs#L101

        // pull requests:
        // https://github.com/dotnet/corefx/pull/40408
        // https://github.com/dotnet/corefx/pull/38322
        // https://github.com/dotnet/corefx/pull/32268

        public static bool AreEqual(JsonContent a, JsonContent b)
        {
            if (Object.ReferenceEquals(a.Content, b.Content)) return true;
            if (Object.ReferenceEquals(a.Content, null)) return false;
            if (Object.ReferenceEquals(b.Content, null)) return false;

            // A JsonContent ultimately represents a json block, so it seems fit to do the comparison that way.
            // also, there's the problem of floating point json writing, that can slightly change between
            // different frameworks.

            var ajson = a.ToJson();
            var bjson = b.ToJson();

            return ajson == bjson;
        }

        [Test]
        public void TestFloatingPointJsonRoundtrip()
        {
            float value = 1.1f; // serialized by system.text.json as 1.1000002f

            var valueTxt = value.ToString("G9", System.Globalization.CultureInfo.InvariantCulture);

            var dict = new Dictionary<string, Object>();            
            dict["value"] = value;            

            JsonContent a = dict;

            // roundtrip to json
            var json = a.ToJson();
            TestContext.Write(json);
            var b = IO.JsonContent.Parse(json);            

            Assert.IsTrue(AreEqual(a, b));            
        }

        [Test]
        public void CreateJsonContent()
        {
            var dict = new Dictionary<string, Object>();
            dict["author"] = "me";
            dict["integer1"] = 17;

            dict["bool1"] = true;

            dict["single1"] = 15.3f;
            dict["single2"] = 1.1f;
            dict["single3"] = -1.1f;
            // dict["singlePI"] = (float)Math.PI; // Fails on .Net Framework 471

            dict["double1"] = 15.3;
            dict["double2"] = 1.1;
            dict["double3"] = -1.1;
            dict["doublePI"] = Math.PI;

            dict["array1"] = new int[] { 1, 2, 3 };
            dict["dict1"] = new Dictionary<string, int> { ["a0"] = 2, ["a1"] = 3 };
            dict["dict2"] = new Dictionary<string, Object>
            {
                ["a"] = 16,
                ["b"] = "delta",
                ["c"] = new List<int>() { 4, 6, 7 },
                ["d"] = new Dictionary<string, int> { ["a0"] = 1, ["a1"] = 2 }
            };            

            JsonContent a = dict;
            
            // roundtrip to json
            var json = a.ToJson();
            TestContext.Write(json);
            var b = IO.JsonContent.Parse(json);            

            // roundtrip to a runtime object
            var x = a.Deserialize(typeof(_TestStructure));
            var c = JsonContent.Serialize(x);

            Assert.IsTrue(AreEqual(a, b));
            Assert.IsTrue(AreEqual(a, c));

            foreach (var dom in new[] { a, b, c})
            {
                Assert.AreEqual("me", dom.GetValue<string>("author"));
                Assert.AreEqual(17, dom.GetValue<int>("integer1"));
                Assert.AreEqual(15.3f, dom.GetValue<float>("single1"));
                Assert.AreEqual(3, dom.GetValue<int>("array1", 2));
                Assert.AreEqual(2, dom.GetValue<int>("dict2", "d", "a1"));
            }            

        }

    }
}
