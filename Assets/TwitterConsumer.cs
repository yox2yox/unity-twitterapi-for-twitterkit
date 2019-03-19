using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using System;
using System.Web;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TwitterKit.Unity;
using System.Security.Cryptography;


/// <summary>
/// ツイッターAPI操作用クラス
/// TwitterKit(https://assetstore.unity.com/packages/tools/integration/twitter-kit-for-unity-84914)が必要
/// GameObjectにComponentとして張り付けて、_apiKeyおよび_apiSecretをInspectorビューから入力してください
/// </summary>
public class TwitterConsumer : MonoBehaviour
{

    [SerializeField]
    //API Key(Consumer Key)：Twitter Application Management から取得する
    private string _apiKey;
    [SerializeField]
    //API Secret(Consumer Secret)：Twitter Application Management から取得する
    private string _apiSecret;
    //Login成功時に返ってくるツイッターアクセストークン
    private AuthToken _token;
    //ツイッターAPIリクエストに必要なパラメータ
    private Dictionary<string, string> _requestParams = new Dictionary<string, string> {
            { "oauth_token","" },
            { "oauth_consumer_key",""},
            { "oauth_signature_method","HMAC-SHA1"},
            { "oauth_timestamp",""},
            { "oauth_nonce",""},
            { "oauth_version","1.0"}
        };

    public static TwitterSession Session {
        get {
            return Twitter.Session;
        }
    }

    public static void Logout() {
        Twitter.LogOut();
    }

    public void Start()
    {
        Twitter.Init();
    }

    /// <summary>
    /// ツイッターにログイン
    /// 成功時、返ってきたアクセストークンを保存する
    /// </summary>
    /// <param name="loginComplete">ログイン成功時の処理</param>
    /// <param name="loginFailure">ログイン失敗時の処理</param>
    public void LogInTwitter(Action<TwitterSession> loginComplete, Action<ApiError> loginFailure) {

        TwitterSession session = Twitter.Session;
        if (session == null)
        {
            Twitter.LogIn((response)=> {
                _token = response.authToken;
                loginComplete(response);
            }, loginFailure);
        }
        else
        {
            _token = session.authToken;
            loginComplete(session);
        }
    }

    public TwitterSession GetSavedTwitterSession() {
        var session = Twitter.Session;
        if (session != null) {
            this._token = session.authToken;
        }
        return session;
    }


    /// <summary>
    /// ユーザー情報取得
    /// LogInTwitterを呼び出す前に呼び出すと、失敗用コールバックを呼び出して終了する
    /// </summary>
    /// <param name="screenName">情報を取得したいユーザーのスクリーンネーム</param>
    /// <param name="complete">リクエスト成功時の処理</param>
    /// <param name="failure">リクエスト失敗時の処理</param>
    public void UsersShow(string screenName,UnityAction<TwitterUsersShowResponse> complete, UnityAction<string> failure)
    {
        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret) || _token == null) {
            failure("You have to login");
            return;
        }

        string requestMethod = "GET";
        string requestUrl = "https://api.twitter.com/1.1/users/show.json";

        var addParams = new Dictionary<string, string> {
            { "screen_name",screenName}
        };

        var reqParams = _GenerateRequestParams(addParams,_apiKey,_token.token);

        string signature = _GenerateSignature(reqParams,_apiSecret,_token.secret,requestMethod,requestUrl);

        reqParams.Add("oauth_signature",signature);
        string header = _HttpBuildQuery(reqParams,",");

        IEnumerator coroutine = _HttpRequestAsync(requestMethod,requestUrl+"?screen_name="+screenName,header,(response)=> {
            complete(JsonUtility.FromJson<TwitterUsersShowResponse>(response));
        },failure);
        StartCoroutine(coroutine);
    }

    /// <summary>
    /// ツイート
    /// LogInTwitterを呼び出す前に呼び出すと、失敗用コールバックを呼び出して終了する
    /// </summary>
    /// <param name="text">ツイートする文章</param>
    /// <param name="complete">リクエスト成功時の処理</param>
    /// <param name="failure">リクエスト失敗時の処理</param>
    public void Tweet(string text, UnityAction complete, UnityAction<string> failure)
    {
        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret) || _token == null)
        {
            failure("You have to login");
            return;
        }

        string requestMethod = "POST";
        string requestUrl = "https://api.twitter.com/1.1/statuses/update.json";


        var addParams = new Dictionary<string, string>
        {
            { "status","%E3%81%93%E3%82%93%E3%81%AB%E3%81%A1%E3%81%AF" }
        };


        var reqParams = _GenerateRequestParams(addParams, _apiKey, _token.token);

        string signature = _GenerateSignature(reqParams, _apiSecret, _token.secret, requestMethod, requestUrl);

        reqParams.Add("oauth_signature", signature);
        string header = _HttpBuildQuery(reqParams, ",");

        IEnumerator coroutine = _HttpRequestAsync(requestMethod, requestUrl+ "?status=%E3%81%93%E3%82%93%E3%81%AB%E3%81%A1%E3%81%AF", header, (response) => {
            complete();
        }, failure);
        StartCoroutine(coroutine);
    }

    /// <summary>
    /// 署名を生成する
    /// </summary>
    /// <param name="paramsDict">署名生成用パラメータ</param>
    /// <param name="apiSecret">APIシークレット</param>
    /// <param name="tokenSecret">トークンシークレット</param>
    /// <param name="requestMethod">リクエストメソッド</param>
    /// <param name="requestUrl">リクエストURL</param>
    private string _GenerateSignature(Dictionary<string, string> paramsDict,string apiSecret,string tokenSecret,string requestMethod,string requestUrl) {

        List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>(paramsDict);
        pairs.Sort((x, y) => { return string.Compare(x.Key, y.Key); });
        string requestParams = _HttpBuildQuery(pairs);
        requestParams = _URLEncode(requestParams);
        string encodedRequestMethod = _URLEncode(requestMethod);
        string encodedRequestUrl = _URLEncode(requestUrl);

        string signatureData = encodedRequestMethod + "&" + encodedRequestUrl + "&" + requestParams;
        string signitureKey = _URLEncode(apiSecret) + "&" + _URLEncode(tokenSecret);

        var hashMethod = new HMACSHA1();
        hashMethod.Key = System.Text.Encoding.ASCII.GetBytes(signitureKey);
        var hash = hashMethod.ComputeHash(System.Text.Encoding.ASCII.GetBytes(signatureData));
        string signature = Convert.ToBase64String(hash);
        signature = _URLEncode(signature);
        return signature;

    }

    /// <summary>
    /// リクエスト用パラメータリストを生成する
    /// </summary>
    /// <param name="addParams">追加パラメータリスト</param>
    /// <param name="apiKey">APIキー</param>
    /// <param name="token">トークン</param>
    private Dictionary<string, string> _GenerateRequestParams(Dictionary<string, string> addParams,string apiKey,string token) {
        var reqParam = new Dictionary<string, string>(_requestParams);
        reqParam["oauth_token"] = token;
        reqParam["oauth_consumer_key"] = apiKey;
        reqParam["oauth_timestamp"] = ((uint)((DateTime.UtcNow.Ticks - DateTime.Parse("1970-01-01 00:00:00").Ticks) / 10000000)).ToString();
        reqParam["oauth_nonce"] = ((uint)((DateTime.UtcNow.Ticks - DateTime.Parse("1970-01-01 00:00:00").Ticks) / 10)).ToString();

        foreach (KeyValuePair<string, string> pair in addParams) {
            reqParam.Add(pair.Key,pair.Value);
        }
        return reqParam;
    }

    /// <summary>
    /// クエリ文字列を生成する
    /// </summary>
    /// <param name="target">対象リスト</param>
    /// <param name="sep">区切り文字（デフォルト'&'）</param>
    private string _HttpBuildQuery(List<KeyValuePair<string, string>> target,string sep="&") {
        string query = "";
        foreach (KeyValuePair<string, string> param in target)
        {
            if (query != "")
            {
                query += sep;
            }

            query += param.Key + "=" + param.Value;
        }
        return query;
    }

    /// <summary>
    /// クエリ文字列を生成する
    /// </summary>
    /// <param name="target">対象ディクショナリー</param>
    /// <param name="sep">区切り文字（デフォルト'&'）</param>
    private string _HttpBuildQuery(Dictionary<string, string> target, string sep = "&")
    {
        var targetlist = new List<KeyValuePair<string, string>>(target);
        return _HttpBuildQuery(targetlist, sep);
    }

    /// <summary>
    /// 文字列のURLエンコードを行う
    /// </summary>
    /// <param name="str">対象文字列</param>
    private string _URLEncode(string str) {
        string url = WWW.EscapeURL(str);
        return _UrlEncodeUpper(url);
    }

    /// <summary>
    /// 文字列のURLエンコードを行う
    /// </summary>
    /// <param name="str">対象文字列</param>
    private string _URLEncode(string str,Encoding encode)
    {
        string url = WWW.EscapeURL(str,encode);
        return _UrlEncodeUpper(url);
    }

    /// <summary>
    /// '%'の後ろ2文字を大文字に変換する
    /// </summary>
    /// <param name="str">対象文字列</param>
    private string _UrlEncodeUpper(string str)
    {
        int p = str.IndexOf("%");
        if (p != -1)
        {
            str = str.Substring(0, p) + str.Substring(p, 3).ToUpper() + _UrlEncodeUpper(str.Substring(p + 3));
        }
        return str;
    }

    /// <summary>
    /// HttpRequestを行う
    /// </summary>
    /// <param name="requestMethod">リクエストメソッド</param>
    /// <param name="requestUrl">リクエストURL</param>
    /// <param name="oAuthHeader">OAuth認証用ヘッダー文字列</param>
    /// <param name="requestComplete">リクエスト成功時の処理</param>
    /// <param name="requestFailuere">リクエスト失敗時の処理</param>
    private IEnumerator _HttpRequestAsync(string requestMethod, string requestUrl, string oAuthHeader, UnityAction<string> requestComplete, UnityAction<string> requestFailure,Dictionary<string,string> postData=null)
    {
        Debug.unityLogger.Log("TwitterConsumer", "Start Twitter Request : " + requestMethod + " " + requestUrl + "\n" + oAuthHeader);
        UnityWebRequest request;

        if (requestMethod == "POST")
        {
            Debug.unityLogger.Log("TwitterConsumer","Ready To Post!");
            request = UnityWebRequest.Post(requestUrl,postData);

        }
        else
        {
            Debug.unityLogger.Log("TwitterConsumer", "Ready To Get!");
            request = UnityWebRequest.Get(requestUrl);
        }
        request.SetRequestHeader("Authorization", "OAuth " + oAuthHeader);
        yield return request.Send();
        if (request.isNetworkError)
        {
            Debug.unityLogger.Log("TwitterConsumer", "Request Failure : \n" + request.error);
            requestFailure(request.error);
        }
        else
        {
            Debug.unityLogger.Log("TwitterConsumer", "Request Complete! : \n" + request.downloadHandler.text);
            requestComplete(request.downloadHandler.text);
        }

    }

}
