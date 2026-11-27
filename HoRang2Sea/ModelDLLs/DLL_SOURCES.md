# Sea 선박 DLL 출처

이 리포는 `*.dll` 을 git 에 넣지 않는다(.gitignore). 설치본에 들어가는 DLL 이 어느 판인지 여기에 적는다.
앱 코드(`Models/*/…Model.cs` 의 `LayoutDlls`)는 파일 이름과 모델 이름(`{파일명에서 _win64.dll 뺀 것}`)으로 찾는다.

## 지금 판 — 한국해양대학교 2026-11-25 송부 (받은자료들/해양대/20261125/KMOU_261125.zip)

받은 파일 이름은 `XX_261125_<레이아웃>_win64.dll` 이지만 진입점 접두어는 261030 그대로라 앱 이름(`XX_261030_…`)으로 바꿔 넣었다(v2.16).

| 파일 | md5 (앞 12자) | PE 빌드 (KST) |
|---|---|---|
| FS_261030_Base_win64.dll | f18bdd369edb | 11-25 16:10 |
| FS_261030_Control_win64.dll | 02fe26d3682b | 11-25 16:16 |
| FS_261030_Design_win64.dll | 28318d7e0813 | 11-25 16:18 |
| FS_261030_Integrated_win64.dll | 75b4052f1a00 | 11-25 16:19 |
| PGS_261030_Base_win64.dll | d3e0f1229bd1 | 11-25 16:22 |
| PGS_261030_Control_win64.dll | 87a8ec7725af | 11-25 16:23 |
| PGS_261030_Design_win64.dll | b7288e55af2a | 11-25 16:25 |
| PGS_261030_Integrated_win64.dll | 1362952dc351 | 11-25 16:27 |
| TS_261030_Base_win64.dll | 8941b8bec4e8 | 11-25 16:29 |
| TS_261030_Control_win64.dll | bee0d0be6bb7 | 11-25 16:31 |
| TS_261030_Design_win64.dll | 2751ec992112 | 11-25 16:34 |
| TS_261030_Integrated_win64.dll | 35ca5b98641f | 11-25 16:35 |

12개 모두 `_step` 하나, 입력 In1~In64, 출력 Out1~Out55. 해양대 11/25: 레이아웃 수정(항만안내선 캐소드 압력 4.2 · 11.1 bar → 1.5 bar 아래), 출력 14 이름 = Total mass flow rate(hydrogen)(확인표 답 2번).

## 이전 판 — 한국해양대학교 2026-10-30 송부 (v2.14, v2.15) (받은자료들/해양대/20261030/KMOU_261030.zip)

| 파일 | md5 (앞 12자) | PE 빌드 (KST) |
|---|---|---|
| FS_261030_Base_win64.dll | a92693c9fea6 | 10-30 18:23 |
| FS_261030_Design_win64.dll | 8b35a43ebf37 | 10-30 18:28 |
| FS_261030_Control_win64.dll | 296e9e3240d8 | 10-30 18:26 |
| FS_261030_Integrated_win64.dll | 96309e1efbba | 10-30 18:30 |
| PGS_261030_Base_win64.dll | 63f732e65aab | 10-30 18:31 |
| PGS_261030_Design_win64.dll | 482236ad39b8 | 10-30 18:36 |
| PGS_261030_Control_win64.dll | 315c0c411d6a | 10-30 18:33 |
| PGS_261030_Integrated_win64.dll | 3c213c8b96b2 | 10-30 18:37 |
| TS_261030_Base_win64.dll | d73c9731b4a0 | 10-30 18:38 |
| TS_261030_Design_win64.dll | 6cd979e39359 | 10-30 18:41 |
| TS_261030_Control_win64.dll | 3ad426341371 | 10-30 18:40 |
| TS_261030_Integrated_win64.dll | 73ae40ee8d33 | 10-30 18:42 |

12개 모두 `_step` 하나, 입력 In1~In64, 출력 Out1~Out55 (포트 이름에 접두어 없음). 해양대 변경내역.pptx: 항만안내선 Control 출력 12 · 41 포트 수정 · Control 을 조정 가능형(_P) → Inline 빌드, 인터쿨러 질량 유량 고정 해결, 수소 총 유량 mol/s → kg/s, 입력 12 · 13 · 18 · 21 · 22 엑셀 고정값.

## 이전 판

- v2.13 까지: 2026-09-18 재빌드(FS_260910_Base 등) — 아래 표

### 2026-09-18 재빌드 (받은자료들/해양대/20260918/260910_Rebuild.zip)

| 파일 | md5 (앞 12자) | PE 빌드 (KST) |
|---|---|---|
| FS_260910_Base_win64.dll | 672381148da6 | 09-18 16:08 |
| FS_260910_Design_win64.dll | 32e9213c01dc | 09-18 16:11 |
| FS_260910_Control_win64.dll | e961b313f054 | 09-18 16:10 |
| FS_260910_Integrated_win64.dll | 75d8be96ec40 | 09-18 16:15 |
| PGS_260910_Base_win64.dll | c343a4bce7a7 | 09-18 17:19 |
| PGS_260910_Design_win64.dll | 0a59a70a1d43 | 09-18 16:27 |
| PGS_260910_Control_win64.dll | e7f87510a2b5 | 09-18 17:23 |
| PGS_260910_Integrated_win64.dll | b25d86a3904a | 09-18 16:32 |
| TS_260910_Base_win64.dll | 028266cad374 | 09-18 18:31 |
| TS_260910_Design_win64.dll | ed128770952f | 09-18 17:01 |
| TS_260910_Control_win64.dll | 7bba4f672d08 | 09-18 16:59 |
| TS_260910_Integrated_win64.dll | e766aa27b73f | 09-18 17:03 |

12개 모두 `_step` 하나, 입력 In1~In64, 출력 Out1~Out55 (포트 이름에 접두어 없음). 어선 Base 도 이 판부터 열린다.
알려진 것(2026-09-19 확인): 항만안내선 Control 은 출력 12(캐소드 압력)와 41(모터 전력)이 아직 옛 단위, 인터쿨러 45 는 12종 모두 0.05 고정.

## 이전 판

- v2.10 (Sea `6de6f60`): 해양대 2026-09-15 빌드 — 배포자료/02_대학_회신/_지난판/2026-09-19_대학별_회신/_근거/해양대0918검토/앱탑재_구DLL_v2.10
- v2.9 이전: 2026-08-14 빌드(`FS_Standard_EffControl.dll` 등) — 배포자료/02_대학_회신/_지난판/2026-09-17_대학별_회신/_근거/해양대0915검토/앱탑재_구DLL_v2.9
