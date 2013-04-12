/**
 * Copyright 2012-2013 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 16-8-2012
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.DataTypes;

namespace MARC.HI.EHRS.CR.Core.Util
{
    /// <summary>
    /// Soundex utility
    /// </summary>
    public static class SoundexUtil
    {

        /// <summary>
        /// Returns true if the name is a soundex match
        /// </summary>
        public static bool IsSoundexMatch(this NameSet ns, NameSet other)
        {
            bool isMatch = true;
            foreach (var cmp in ns.Parts)
                isMatch &= ns.Parts.Exists(o => other.Parts.Exists(p => p.CalculateSoundexCode() == o.CalculateSoundexCode() && o.Type == p.Type));
            return isMatch;

        }

        /// <summary>
        /// Confidence equals
        /// </summary>
        public static float ConfidenceEquals(this NameSet ns, NameSet other)
        {

            // Matches
            int nExact = other.Parts.Count(o => ns.Parts.Exists(p => p.Value.ToLower() == o.Value.ToLower() && p.Type == o.Type)),
                nSoundex = other.Parts.Count(o => ns.Parts.Exists(p => p.CalculateSoundexCode() == o.CalculateSoundexCode() && o.Type == p.Type)) - nExact,
                nPattern = other.Parts.Count(o=>ns.Parts.Exists(p=>o.Value.Contains("*") && p.Value.ToLower() != o.Value.ToLower() && p.Value.ToLower().StartsWith(o.Value.ToLower().Substring(0, o.Value.IndexOf("*"))) && o.Type == p.Type)),
                nOthers = other.Parts.Count - nExact - nSoundex - nPattern,
                nParts = other.Parts.Count;

            return nExact / (float)nParts + nPattern / (float)(nParts * 1.25) + nSoundex / (float)(nParts * 1.5f) + nOthers / ((float)nParts * 2);

        }

        /// <summary>
        /// Determine if this is a soundex match
        /// </summary>
        public static string CalculateSoundexCode(this NamePart nc)
        {
            StringBuilder soundex = new StringBuilder();

            if (!String.IsNullOrEmpty(nc.Value))
            {
                int prevCode = 0, currentCode = 0;
                soundex.Append(nc.Value[0]);

                foreach (char letter in nc.Value.Substring(1).ToLower())
                {
                    if ("bpfv".Contains(letter))
                        currentCode = 1;
                    else if ("cgjkqsxz".Contains(letter))
                        currentCode = 2;
                    else if ("dt".Contains(letter))
                        currentCode = 3;
                    else if (letter == 'l')
                        currentCode = 4;
                    else if ("mn".Contains(letter))
                        currentCode = 5;
                    else if (letter == 'r')
                        currentCode = 6;

                    if (currentCode != prevCode)
                        soundex.Append(currentCode);
                    if (soundex.Length == 4) break;

                    if (currentCode != 0)
                        prevCode = currentCode;
                }

            }

            // Pad
            if(soundex.Length < 4)
                soundex.Append(new String('0', 4 - soundex.Length));

            return soundex.ToString().ToUpper();

        }

        

    }
}
