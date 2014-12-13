using System;
using Kekiri;
using FluentAssertions;
using Rangic.Utilities.Preferences;
using Newtonsoft.Json.Linq;

namespace UnitTests.Prefs
{
    [Scenario(Feature.Preferences, "Preferences")]
    public class PreferencesTests : FluentTest
    {
        const string filename = "TestPrefs.json";
        private TestPrefs originalPrefs;

        public PreferencesTests()
        {
            Given(A_preferences_class);
            When(Preferences_are_saved);
            Then(Preferences_can_be_properly_loaded);
        }

        private void A_preferences_class()
        {
            originalPrefs = Preferences<TestPrefs>.Load(filename);
            originalPrefs.ValInt = 33;
            originalPrefs.ValString = "Over here!";
        }

        private void Preferences_are_saved()
        {
            Preferences<TestPrefs>.Save();
        }

        private void Preferences_can_be_properly_loaded()
        {
            var loadedPrefs = Preferences<TestPrefs>.Load(filename);
            loadedPrefs.ValInt.Should().Be(originalPrefs.ValInt);
            loadedPrefs.ValString.Should().Be(originalPrefs.ValString);
        }
    }

    public class TestPrefs : BasePreferences
    {
        public string ValString { get; set; }
        public int ValInt { get; set; }

        public override void FromJson(dynamic json)
        {
            ValString = json.ValString;
            ValInt = json.ValInt;
        }

        public override dynamic ToJson()
        {
            return new
            {
                ValString,
                ValInt
            };
        }
    }
}

