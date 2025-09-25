#if UNITY_ANDROID || UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Linq;

namespace Crosstales.RTVoice.Provider
{
   /// <summary>Android voice provider.</summary>
   public class VoiceProviderAndroid : BaseVoiceProvider<VoiceProviderAndroid>
   {
      #region Variables

      private static string lastEngine;
      private static bool isInitialized;
      private static AndroidJavaObject ttsHandler;

      private static bool isSSML; //>M (API level 23) = true

      private readonly WaitForSeconds wfs = new WaitForSeconds(0.1f);

      private System.Collections.Generic.List<string> cachedEngines = new System.Collections.Generic.List<string>();
      private bool isLoading;

      private static readonly string[] androidMales =
      {
         "ar-xa-x-ard",
         "ar-xa-x-are",
         "bn-BD-language",
         "bn-bd-x-ban",
         "bn-in-x-bin",
         "bn-in-x-bnm",
         "cmn-cn-x-ccd",
         "cmn-cn-x-cce",
         "cmn-tw-x-ctd",
         "cmn-tw-x-cte",
         "da-dk-x-nmm",
         "de-de-x-deb",
         "de-de-x-deg",
         "en-au-x-aub",
         "en-au-x-aud",
         "en-gb-x-gbb",
         "en-gb-x-gbd",
         "en-gb-x-rjs",
         "en-in-x-end",
         "en-in-x-ene",
         "en-us-x-iol",
         "en-us-x-iom",
         "en-us-x-tpd",
         "es-es-x-eed",
         "es-es-x-eef",
         "es-us-x-esd",
         "es-us-x-esf",
         "et-EE-language",
         "et-ee-x-tms",
         "fil-ph-x-fid",
         "fil-ph-x-fie",
         "fr-ca-x-cab",
         "fr-ca-x-cad",
         "fr-fr-x-frb",
         "fr-fr-x-frd",
         "gu-in-x-gum",
         "hi-in-x-hid",
         "hi-in-x-hie",
         "id-id-x-idd",
         "id-id-x-ide",
         "it-it-x-itc",
         "it-it-x-itd",
         "ja-jp-x-jac",
         "ja-jp-x-jad",
         "kn-in-x-knm",
         "ko-kr-x-koc",
         "ko-kr-x-kod",
         "ml-in-x-mlm",
         "ms-my-x-msd",
         "ms-my-x-msg",
         "nb-no-x-cmj",
         "nb-no-x-tmg",
         "nl-nl-x-bmh",
         "nl-nl-x-dma",
         "pl-pl-x-bmg",
         "pl-pl-x-jmk",
         "pt-pt-x-jmn",
         "pt-pt-x-pmj",
         "ru-ru-x-rud",
         "ru-ru-x-ruf",
         "ta-in-x-tag",
         "te-in-x-tem",
         "tr-tr-x-ama",
         "tr-tr-x-tmc",
         "ur-pk-x-urm",
         "vi-vn-x-vid",
         "vi-vn-x-vif",
         "yue-hk-x-yud",
         "yue-hk-x-yuf"
      };

      #endregion


      #region Properties

/*
      /// <summary>Returns the singleton instance of this class.</summary>
      /// <returns>Singleton instance of this class.</returns>
      public static VoiceProviderAndroid Instance => instance ?? (instance = new VoiceProviderAndroid());
*/
      public override string AudioFileExtension => ".wav";

      public override AudioType AudioFileType => AudioType.WAV;

      //public override string DefaultVoiceName => "English (United States)";

      public override bool isWorkingInEditor => false;

      public override bool isWorkingInPlaymode => false;

      public override int MaxTextLength => 3999;

      public override bool isSpeakNativeSupported => true;

      public override bool isSpeakSupported => true;

      public override bool isPlatformSupported => Crosstales.RTVoice.Util.Helper.isAndroidPlatform;

      public override bool isSSMLSupported => isSSML;

      public override bool isOnlineService => false;

      public override bool hasCoRoutines => true;

      public override bool isIL2CPPSupported => true;

      public override bool hasVoicesInEditor => false;

      /// <summary> Returns all installed TTS engines on Android.</summary>
      public System.Collections.Generic.List<string> Engines => cachedEngines;

      public override int MaxSimultaneousSpeeches => 0;

      #endregion


      #region Constructor

      public VoiceProviderAndroid()
      {
#if !UNITY_EDITOR || CT_DEVELOP
         if (!isInitialized)
            initializeTTS();
#endif
      }

      #endregion


      #region Implemented methods

      public override void Load(bool forceReload = false)
      {
#if !UNITY_EDITOR || CT_DEVELOP
         bool _forceReload = forceReload;

         if (lastEngine != Speaker.Instance.AndroidEngine)
         {
            instance = this;
            isInitialized = false;
            _forceReload = true;
         }

         if (!isInitialized)
            initializeTTS();

         if (cachedVoices?.Count == 0 || _forceReload)
         {
            if (!isLoading)
            {
               isLoading = true;
               Speaker.Instance.StartCoroutine(getVoices());
            }
         }
         else
         {
            onVoicesReady();
         }
#endif
      }

      public override IEnumerator SpeakNative(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
#if !UNITY_EDITOR || CT_DEVELOP
         if (wrapper == null)
         {
            Debug.LogWarning("'wrapper' is null!");
         }
         else
         {
            if (string.IsNullOrEmpty(wrapper.Text))
            {
               Debug.LogWarning("'wrapper.Text' is null or empty!");
            }
            else
            {
               yield return null; //return to the main process (uid)

               if (!isInitialized)
               {
                  do
                  {
                     // waiting...
                     yield return wfs;
                  } while (!(isInitialized = ttsHandler.CallStatic<bool>("isInitialized")));
               }

               string voiceName = getVoiceName(wrapper);
               silence = false;
               onSpeakStart(wrapper);

               ttsHandler.CallStatic("SpeakNative", prepareText(wrapper), wrapper.Rate, wrapper.Pitch, wrapper.Volume, voiceName);

               do
               {
                  yield return wfs;
               } while (!silence && ttsHandler.CallStatic<bool>("isWorking"));

               if (Crosstales.RTVoice.Util.Config.DEBUG)
                  Debug.Log("Text spoken: " + wrapper.Text);

               onSpeakComplete(wrapper);
            }
         }
#else
            yield return null;
#endif
      }

      public override IEnumerator Speak(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
#if !UNITY_EDITOR || CT_DEVELOP
         if (wrapper == null)
         {
            Debug.LogWarning("'wrapper' is null!");
         }
         else
         {
            if (string.IsNullOrEmpty(wrapper.Text))
            {
               Debug.LogWarning("'wrapper.Text' is null or empty: " + wrapper);
            }
            else
            {
               if (wrapper.Source == null)
               {
                  Debug.LogWarning("'wrapper.Source' is null: " + wrapper);
               }
               else
               {
                  yield return null; //return to the main process (uid)

                  if (!isInitialized)
                  {
                     do
                     {
                        // waiting...
                        yield return wfs;
                     } while (!(isInitialized = ttsHandler.CallStatic<bool>("isInitialized")));
                  }

                  string voiceName = getVoiceName(wrapper);
                  string outputFile = getOutputFile(wrapper.Uid, true);

                  ttsHandler.CallStatic<string>("Speak", prepareText(wrapper), wrapper.Rate, wrapper.Pitch, voiceName, outputFile);

                  silence = false;
                  onSpeakAudioGenerationStart(wrapper);

                  do
                  {
                     yield return wfs;
                  } while (!silence && ttsHandler.CallStatic<bool>("isWorking"));

                  yield return playAudioFile(wrapper, Crosstales.Common.Util.NetworkHelper.GetURLFromFile(outputFile), outputFile);
               }
            }
         }
#else
            yield return null;
#endif
      }

      public override IEnumerator Generate(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
#if !UNITY_EDITOR || CT_DEVELOP
         if (wrapper == null)
         {
            Debug.LogWarning("'wrapper' is null!");
         }
         else
         {
            if (string.IsNullOrEmpty(wrapper.Text))
            {
               Debug.LogWarning("'wrapper.Text' is null or empty: " + wrapper);
            }
            else
            {
               yield return null; //return to the main process (uid)

               if (!isInitialized)
               {
                  do
                  {
                     // waiting...
                     yield return wfs;
                  } while (!(isInitialized = ttsHandler.CallStatic<bool>("isInitialized")));
               }

               string voiceName = getVoiceName(wrapper);
               string outputFile = getOutputFile(wrapper.Uid, true);

               ttsHandler.CallStatic<string>("Speak", prepareText(wrapper), wrapper.Rate, wrapper.Pitch, voiceName, outputFile);

               silence = false;
               onSpeakAudioGenerationStart(wrapper);

               do
               {
                  yield return wfs;
               } while (!silence && ttsHandler.CallStatic<bool>("isWorking"));

               processAudioFile(wrapper, outputFile);
            }
         }
#else
            yield return null;
#endif
      }

#if !UNITY_EDITOR || CT_DEVELOP
      public override void Silence()
      {
         ttsHandler.CallStatic("StopNative");

         base.Silence();
      }
#endif

      #endregion


      #region Public methods

      public static void ShutdownTTS()
      {
#if !UNITY_EDITOR || CT_DEVELOP
         ttsHandler.CallStatic("Shutdown");
#endif
      }

      #endregion


      #region Private methods

#if !UNITY_EDITOR || CT_DEVELOP
      private IEnumerator getVoices()
      {
         yield return null;

         if (!isInitialized)
         {
            do
            {
               yield return wfs;
            } while (!(isInitialized = ttsHandler.CallStatic<bool>("isInitialized")));
         }

         string[] stringVoices = null;
         bool success = false;

         try
         {
            stringVoices = ttsHandler.CallStatic<string[]>("GetVoices");
            success = true;
         }
         catch (System.Exception ex)
         {
            string errorMessage = "Could not get any voices!" + System.Environment.NewLine + ex;
            Debug.LogError(errorMessage);
            onErrorInfo(null, errorMessage);
         }

         if (success)
         {
            System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice> voices = new System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice>(600);

            foreach (string voice in stringVoices)
            {
               string[] currentVoiceData = voice.Split(';');

               if (!currentVoiceData[0].CTContains("network")) //ignore network-voices
               {
                  Crosstales.RTVoice.Model.Enum.Gender gender = Crosstales.RTVoice.Model.Enum.Gender.UNKNOWN;

                  if (currentVoiceData[0].CTContains("#male"))
                  {
                     gender = Crosstales.RTVoice.Model.Enum.Gender.MALE;
                  }
                  else if (currentVoiceData[0].CTContains("#female"))
                  {
                     gender = Crosstales.RTVoice.Model.Enum.Gender.FEMALE;
                  }

                  if (gender == Crosstales.RTVoice.Model.Enum.Gender.UNKNOWN)
                  {
                  }

                  string name = currentVoiceData[0];
                  voices.Add(new Crosstales.RTVoice.Model.Voice(name, "Android voice: " + voice, getGender(name), Crosstales.RTVoice.Util.Constants.VOICE_AGE_UNKNOWN, currentVoiceData[1], "", "unknown", 0, isNeural(name)));
               }
            }

            cachedVoices = voices.OrderBy(s => s.Name).ToList();

            if (Crosstales.RTVoice.Util.Constants.DEV_DEBUG)
               Debug.Log("Voices read: " + cachedVoices.CTDump());
         }

         yield return getEngines();

         isLoading = false;

         onVoicesReady();
      }

      private IEnumerator getEngines()
      {
         string[] stringEngines = null;
         bool success = false;

         try
         {
            stringEngines = ttsHandler.CallStatic<string[]>("GetEngines");
            success = true;
         }
         catch (System.Exception ex)
         {
            string errorMessage = "Could not get any engines!" + System.Environment.NewLine + ex;
            Debug.LogWarning(errorMessage);
            onErrorInfo(null, errorMessage);
         }

         if (success)
         {
            yield return null;

            System.Collections.Generic.List<string> engines = stringEngines.Select(voice => voice.Split(';')).Select(currentEngineData => currentEngineData[0]).ToList();

            cachedEngines = engines.OrderBy(s => s).ToList();

            if (Crosstales.RTVoice.Util.Constants.DEV_DEBUG)
               Debug.Log("Engines read: " + cachedEngines.CTDump());
         }
      }

      private static void initializeTTS()
      {
         AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
         AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
         ttsHandler = new AndroidJavaObject("com.crosstales.RTVoice.RTVoiceAndroidBridge", jo);
         ttsHandler.CallStatic("SetupEngine", Speaker.Instance.AndroidEngine);

         lastEngine = Speaker.Instance.AndroidEngine;

         isSSML = ttsHandler.CallStatic<bool>("isSSMLSupported");
      }

      private static string prepareText(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         //TEST
         //wrapper.ForceSSML = false;

         if (isSSML && wrapper.ForceSSML && !Speaker.Instance.AutoClearTags)
         {
            System.Text.StringBuilder sbXML = new System.Text.StringBuilder();

            //sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
            //sbXML.Append("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"");
            //sbXML.Append(wrapper.Voice == null ? "en-US" : wrapper.Voice.Culture);
            //sbXML.Append("\">");

            sbXML.Append("<speak>");
/*
            //Volume seems to have no effect!
            
            if (wrapper.Volume < 1f)
            {
               sbXML.Append("<prosody volume='");

               sbXML.Append((1 - wrapper.Volume).ToString("-#0%", Crosstales.RTVoice.Util.Helper.BaseCulture));

               sbXML.Append("'>");
            }
*/
            sbXML.Append(wrapper.Text);
/*
            if (wrapper.Volume < 1f)
               sbXML.Append("</prosody>");
*/
            sbXML.Append("</speak>");

            return getValidXML(sbXML.ToString().Replace('"', '\''));
         }

         return wrapper.Text.Replace('"', '\'');
      }
#endif

      private static bool isNeural(string name)
      {
         return name.CTContains("wavenet") || name.CTContains("neural");
      }

      private static Crosstales.RTVoice.Model.Enum.Gender getGender(string voiceName)
      {
         Crosstales.RTVoice.Model.Enum.Gender gender = Crosstales.RTVoice.Model.Enum.Gender.UNKNOWN;
         if (!string.IsNullOrEmpty(voiceName))
         {
            if (voiceName.CTContains("#male"))
            {
               gender = Crosstales.RTVoice.Model.Enum.Gender.MALE;
            }
            else if (voiceName.CTContains("#female"))
            {
               gender = Crosstales.RTVoice.Model.Enum.Gender.FEMALE;
            }

            if (gender == Crosstales.RTVoice.Model.Enum.Gender.UNKNOWN)
            {
               gender = Crosstales.RTVoice.Model.Enum.Gender.FEMALE; //fallback, 2/3 of the Google TTS under Android 11 are female

               if (androidMales.Any(male => voiceName.CTContains(male)))
                  return Crosstales.RTVoice.Model.Enum.Gender.MALE;
            }
         }

         return gender;
      }

      #endregion


      #region Editor-only methods

#if UNITY_EDITOR

      public override void GenerateInEditor(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         Debug.LogError("'GenerateInEditor' is not supported for Android!");
      }

      public override void SpeakNativeInEditor(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         Debug.LogError("'SpeakNativeInEditor' is not supported for Android!");
      }

#endif

      #endregion
   }
}
#endif
// © 2016-2024 crosstales LLC (https://www.crosstales.com)