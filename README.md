# RagSample

## 프로젝트 개요

이 리포지터리는 Qdrant 벡터 저장소와 Ollama 런타임을 이용하여 검색 증강 생성 (RAG)을 위한 벡터 데이터베이스 인덱싱 방법을 설명하는 코드 샘플을 담고 있습니다. 데이터 셋은 한국어 위키백과 덤프 데이터를 이용합니다.

프로그램은 두 개의 모드로 구분되어 실행됩니다.

- `Build`: `%USERPROFILE%` 디렉터리 아래에 저장된 한국어 위키백과 덤프 데이터 XML 파일을 읽고, Ollama에서 특정 임베딩 모델을 Pull 하여 준비한 다음, '제주'와 관련된 내용만 필터링하여 벡터를 생성한 후 Qdrant 벡터 DB에 데이터를 저장합니다.
- `Search`: `Build` 단계에서 인덱싱이 끝난 벡터 데이터베이스와 `Build` 단계에서 사용한 동일 임베딩 모델을 이용하여 사용자의 질문을 벡터로 변환한 다음, 코사인 유사도 분석 알고리즘으로 일치 정도가 높은 문서 항목들을 반환하는 동작을 수행합니다.

## 설치

다음의 구성 요소를 설치하거나 준비합니다.

- [.NET 9 SDK](https://dot.net/)
- [Ollama](https://ollama.com/download) (설치 후 서버만 시작해도 됩니다. 모델 Pull은 샘플 프로그램 동작 안에 포함됩니다.)
- [Qdrant](https://github.com/qdrant/qdrant/releases/latest) (설치 후 서버만 시작해도 됩니다.)
- [한국어 위키백과 데이터 파일](https://dumps.wikimedia.org/kowiki/latest/) (`kowiki-latest-pages-articles-multistream.xml.bz2` 파일 추천)

## 사용 방법

1. 한국어 위키백과 데이터 파일의 압축을 풀어, `%USERPROFILE%` 디렉터리에 XML 파일을 저장합니다.
1. `appconfig.json` 파일의 내용을 검토하여 적절한 설정 값으로 변경합니다.
1. `dotnet run --mode=Build` 명령으로 벡터 데이터베이스에 인덱싱 데이터를 넣습니다.
1. `dotnet run --mode=Search` 명령으로 문서 검색을 수행합니다.

## 기여 방법

버그 리포트나 제안 사항은 이슈를 통해 등록합니다. 또한, Pull Request로 변경 사항을 기여할 수 있습니다.

## 라이선스

이 프로젝트는 MIT 라이선스를 따릅니다.
