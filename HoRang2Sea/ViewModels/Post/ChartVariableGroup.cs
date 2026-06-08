using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HoRang2Sea.ViewModels
{
    // 변수 선택 다이얼로그(차트 Property)용 컴포넌트 그룹.
    // 출력 변수명의 접두어(첫 '_' 토큰: FCS / BAT / MOT / VS ...)로 묶어 트리로 보여준다.
    // 렌더 파이프라인과 무관한 순수 표시용 구조이며, 모델 변경이 필요 없다.
    public class ChartVariableGroup
    {
        public string GroupName { get; set; }
        public ObservableCollection<string> Items { get; set; } = new ObservableCollection<string>();

        // 변수명 → 그룹 키 (첫 '_' 앞 토큰, 없으면 "기타")
        public static string GroupKeyOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return "기타";
            int idx = name.IndexOf('_');
            return idx > 0 ? name.Substring(0, idx) : "기타";
        }

        // 평면 이름 목록 → 그룹 트리. expandAll=true 면 모든 그룹을 펼친 상태로 생성(검색 시 사용).
        public static ObservableCollection<ChartVariableGroup> Build(IEnumerable<string> names, bool expandAll = true)
        {
            var result = new ObservableCollection<ChartVariableGroup>();
            if (names == null) return result;

            foreach (var g in names.Where(n => !string.IsNullOrEmpty(n))
                                    .GroupBy(GroupKeyOf)
                                    .OrderBy(grp => grp.Key))
            {
                var group = new ChartVariableGroup { GroupName = g.Key, IsExpanded = expandAll };
                foreach (var item in g)
                    group.Items.Add(item);
                result.Add(group);
            }
            return result;
        }

        public bool IsExpanded { get; set; } = true;
    }
}
