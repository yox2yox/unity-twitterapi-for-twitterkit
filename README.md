# unity-twitterapi-for-twitterkit
UnityのアセットTwitter Kitを使って取得したトークンを利用して、TwitterAPIを叩く

# 必要アセット
Twitter Kit for Unity(https://assetstore.unity.com/packages/tools/integration/twitter-kit-for-unity-84914)

# 設定
GameObjectにコンポーネントとして設定

Inspector Viewにおいて

* ApiKey
+ ApiSecret

を設定

ApiKeyおよびApiSecretはTwitter Application Managementから取得のこと

# 使い方
API呼び出しを行う前に必ず、LogInTwitter()を呼び出す。

ログインが成功すれば、Tweet(), UsersShow()などの関数でTwitterAPIの機能を利用できる

# 対応API
|API|メソッド名|
|:-----------|------------:|
|users/show  | UsersShow   |
| tatuses/update | Tweet   |
