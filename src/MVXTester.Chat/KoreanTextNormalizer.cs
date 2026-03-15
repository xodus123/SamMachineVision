using System.Text;

namespace MVXTester.Chat;

/// <summary>
/// 한글 초성 분리(자모 분해) 텍스트를 정상 음절로 재조합하는 유틸리티.
/// 예: "크로ㅂㄴㅗㄷㅡ" → "크롭노드", "프로그래ㅁㅇㅣㅇㅑ" → "프로그램이야"
/// </summary>
public static class KoreanTextNormalizer
{
    // 한글 유니코드 범위
    private const int HangulBase = 0xAC00;  // '가'
    private const int HangulEnd = 0xD7A3;   // '힣'
    private const int JamoChoBase = 0x3131;  // 'ㄱ'
    private const int JamoChoEnd = 0x314E;   // 'ㅎ'
    private const int JamoJungBase = 0x314F; // 'ㅏ'
    private const int JamoJungEnd = 0x3163;  // 'ㅣ'

    // 초성 19자: ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ
    private static readonly char[] ChoTable =
    {
        'ㄱ','ㄲ','ㄴ','ㄷ','ㄸ','ㄹ','ㅁ','ㅂ','ㅃ','ㅅ',
        'ㅆ','ㅇ','ㅈ','ㅉ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ'
    };

    // 중성 21자: ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ
    private static readonly char[] JungTable =
    {
        'ㅏ','ㅐ','ㅑ','ㅒ','ㅓ','ㅔ','ㅕ','ㅖ','ㅗ','ㅘ',
        'ㅙ','ㅚ','ㅛ','ㅜ','ㅝ','ㅞ','ㅟ','ㅠ','ㅡ','ㅢ','ㅣ'
    };

    // 종성 28자 (0=없음): (없음)ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ
    private static readonly char[] JongTable =
    {
        '\0','ㄱ','ㄲ','ㄳ','ㄴ','ㄵ','ㄶ','ㄷ','ㄹ','ㄺ',
        'ㄻ','ㄼ','ㄽ','ㄾ','ㄿ','ㅀ','ㅁ','ㅂ','ㅄ','ㅅ',
        'ㅆ','ㅇ','ㅈ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ'
    };

    private static bool IsChosung(char c) => c >= JamoChoBase && c <= JamoChoEnd;
    private static bool IsJungsung(char c) => c >= JamoJungBase && c <= JamoJungEnd;
    private static bool IsHangulSyllable(char c) => c >= HangulBase && c <= HangulEnd;

    private static int ChoIndex(char c) => Array.IndexOf(ChoTable, c);
    private static int JungIndex(char c) => Array.IndexOf(JungTable, c);
    private static int JongIndex(char c) => Array.IndexOf(JongTable, c);

    /// <summary>
    /// 초성 분리된 한글 텍스트를 정상 음절로 재조합합니다.
    /// 완벽하지 않지만 "크로ㅂㄴㅗㄷㅡ" → "크롭노드" 수준의 복원이 가능합니다.
    /// </summary>
    public static string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var result = new StringBuilder(text.Length);
        var chars = text.ToCharArray();
        int i = 0;

        while (i < chars.Length)
        {
            var c = chars[i];

            // 완성된 한글 음절: 종성 없이 끝난 경우, 다음이 초성이면 종성으로 붙일 수 있는지 확인
            if (IsHangulSyllable(c))
            {
                int code = c - HangulBase;
                int jong = code % 28;

                // 현재 음절에 종성이 없고, 다음이 자모 초성이고, 그 다음이 모음이 아닌 경우 → 종성으로 결합
                if (jong == 0 && i + 1 < chars.Length && IsChosung(chars[i + 1]))
                {
                    int nextJong = JongIndex(chars[i + 1]);
                    if (nextJong > 0)
                    {
                        // 다음 다음 문자가 모음이면 → 종성이 아니라 다음 음절의 초성
                        bool nextIsNewSyllable = i + 2 < chars.Length && IsJungsung(chars[i + 2]);
                        if (!nextIsNewSyllable)
                        {
                            // 종성 결합
                            result.Append((char)(c + nextJong));
                            i += 2;
                            continue;
                        }
                    }
                }
                result.Append(c);
                i++;
                continue;
            }

            // 자모 초성: 다음이 중성이면 음절 조합 시도
            if (IsChosung(c) && i + 1 < chars.Length && IsJungsung(chars[i + 1]))
            {
                int cho = ChoIndex(c);
                int jung = JungIndex(chars[i + 1]);

                if (cho >= 0 && jung >= 0)
                {
                    int jong = 0;
                    int consumed = 2;

                    // 다음이 종성 후보인지 확인
                    if (i + 2 < chars.Length && IsChosung(chars[i + 2]))
                    {
                        int jongCandidate = JongIndex(chars[i + 2]);
                        if (jongCandidate > 0)
                        {
                            // 그 다음이 모음이면 → 종성이 아니라 다음 음절 초성
                            bool isNextSyllableStart = i + 3 < chars.Length && IsJungsung(chars[i + 3]);
                            if (!isNextSyllableStart)
                            {
                                jong = jongCandidate;
                                consumed = 3;
                            }
                        }
                    }

                    char syllable = (char)(HangulBase + cho * 21 * 28 + jung * 28 + jong);
                    result.Append(syllable);
                    i += consumed;
                    continue;
                }
            }

            // 그 외 문자는 그대로
            result.Append(c);
            i++;
        }

        return result.ToString();
    }
}
