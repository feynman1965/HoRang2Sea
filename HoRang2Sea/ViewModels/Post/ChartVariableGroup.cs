using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HoRang2Sea.ViewModels
{
    // 트리 잎: 변수 1개 + 차트 추가 여부(✓ 표시·토글용)
    public class ChartVariableItem
    {
        public string Name { get; set; }
        public bool IsAdded { get; set; }
    }

    // 변수 선택 다이얼로그(차트 Property)용 컴포넌트 그룹.
    // 출력 변수명의 접두어(첫 '_' 토큰: FCS / BAT / MOT / VS ...)로 묶어 트리로 보여준다.
    // 렌더 파이프라인과 무관한 순수 표시용 구조이며, 모델 변경이 필요 없다.
    public class ChartVariableGroup
    {
        public string GroupName { get; set; }
        public ObservableCollection<ChartVariableItem> Items { get; set; } = new ObservableCollection<ChartVariableItem>();
        public bool IsExpanded { get; set; } = true;

        // 변수명 → 그룹 키. 탑재 Output name(언더스코어 없음)은 레지스트리로 컴포넌트 조회,
        // 그 외(구 Simulink명 등)는 첫 '_' 앞 토큰, 없으면 "기타".
        public static string GroupKeyOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Other";
            if (OutputComponentRegistry.Map.TryGetValue(name, out var comp)) return comp;
            int idx = name.IndexOf('_');
            return idx > 0 ? name.Substring(0, idx) : "Other";
        }

        // available(미추가) + added(추가됨)를 합쳐 그룹 트리 생성. added는 ✓ 표시(IsAdded=true).
        // 변수는 추가해도 트리에서 사라지지 않고 그 자리에서 표시만 바뀐다(그룹 내 이름순 정렬로 위치 고정).
        public static ObservableCollection<ChartVariableGroup> Build(IEnumerable<string> available, IEnumerable<string> added, bool expandAll = true)
        {
            var result = new ObservableCollection<ChartVariableGroup>();
            var addedSet = new HashSet<string>((added ?? Enumerable.Empty<string>()).Where(n => !string.IsNullOrEmpty(n)));
            var all = new List<string>();
            if (available != null)
                foreach (var n in available)
                    if (!string.IsNullOrEmpty(n)) all.Add(n);
            foreach (var a in addedSet)
                if (!all.Contains(a)) all.Add(a);

            foreach (var g in all.Distinct().GroupBy(GroupKeyOf).OrderBy(grp => grp.Key))
            {
                var group = new ChartVariableGroup { GroupName = g.Key, IsExpanded = expandAll };
                foreach (var item in g.OrderBy(x => x))
                    group.Items.Add(new ChartVariableItem { Name = item, IsAdded = addedSet.Contains(item) });
                result.Add(group);
            }
            return result;
        }
    }
}
