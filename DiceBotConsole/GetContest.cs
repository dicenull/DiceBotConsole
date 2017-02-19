﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using HtmlAgilityPack;

namespace DiceBotConsole
{
    class GetContest
    {
        HtmlDocument HomePage;
        HtmlDocument ContestPage;

        /// <summary>
        /// Atcoderのコンテスト情報を取得する
        /// </summary>
        /// <returns>Atcoderのコンテスト</returns>
        public List<Contest> GetAtcoderContests(List<Contest> OldContests)
        {
            List<Contest> contests = new List<Contest>();

            HomePage = Scraping("https://atcoder.jp/?lang=ja", "utf-8");

            //予定されたコンテスト部分の情報を取得
            var nodes = HomePage.DocumentNode.SelectNodes("//div[2]/table[@class='table table-default table-striped table-hover table-condensed']/tbody/tr/td[2]/small/a")
                                    .Select(a => new
                                    {
                                        Link = a.Attributes["href"].Value.Trim(),
                                        Name = a.InnerText.Trim(),
                                    });

            //予定されたコンテスト部が存在するか確認
            string title = HomePage.DocumentNode.SelectSingleNode("//h4[2]").InnerText;
            if(!title.Contains("終了"))
            {
                Contest con = new Contest();
                foreach(var node in nodes.Take(nodes.Count()))
                {
                    con = new Contest();

                    // リンクと名前を追加
                    con.Link = node.Link;
                    con.Name = node.Name;


                    // 開始時間と終了時間を取得
                    ContestPage = new HtmlDocument();

                    try
                    {
                        WebClient wc = new WebClient();
                        string html = wc.DownloadString(con.Link + "/?lang=ja");
                        ContestPage.LoadHtml(html);
                    }
                    catch
                    {
                        Console.WriteLine("--- URLが正しく読み取ることが出来ませんでした\a");
                    }

                    var StartTime = ContestPage.DocumentNode.SelectSingleNode(@"/html/body/div[1]/div/div/a[2]/span[2]/time[1]");
                    var EndTime = ContestPage.DocumentNode.SelectSingleNode(@"/html/body/div[1]/div/div/a[2]/span[2]/time[2]");

                    // 開始時間と終了時間を追加
                    con.StartTime = DateTime.Parse(StartTime.InnerText);
                    con.EndTime = DateTime.Parse(EndTime.InnerText);

                    // 新しいコンテストを追加
                    contests.Add(con);
                }
            }
            
            // コンテスト名をABC,ARC,AGCに短縮
            for(int i = 0;i < contests.Count();i++)
            {
                string name = contests[i].Name;
                Dictionary<string, string> Names = new Dictionary<string, string>();

                Names.Add("AtCoder Beginner Contest", "ABC");
                Names.Add("AtCoder Regular Contest", "ARC");
                Names.Add("AtCoder Grand Contest","AGC");

                for(int j = 0;j < Names.Count();j++)
                {
                    if (name.Contains(Names.ElementAt(j).Key))
                    {
                        name = Names.ElementAt(j).Value;
                    }
                }

                contests[i].Name = name;
            }

            // 既存のコンテストなら追加しない
            for(int i = 0;i < OldContests.Count;i++)
            {
                contests.Remove(OldContests[i]);
            }

            // コンテストをまとめる
            contests = SumContest(contests);

            return contests;
        }

        /// <summary>
        /// urlのページからhtml情報を取得する
        /// </summary>
        /// <param name="url">指定するURL</param>
        /// <param name="codepage">文字コード指定</param>
        /// <returns></returns>
        private HtmlDocument Scraping(string url,string codepage)
        {
            HtmlDocument doc = new HtmlDocument();
            WebClient wc = new WebClient();
            string html = "";

            wc.Encoding = Encoding.GetEncoding(codepage);

            try
            {
                html = wc.DownloadString(url);
            }
            catch(WebException)
            {
                Console.WriteLine("--- 無効なURLです\a");
            }

            doc.LoadHtml(html);

            return doc;
        }


        /// <summary>
        /// 開始時間と終了時間が同じであるコンテストをまとめる
        /// </summary>
        /// <param name="contests">まとめたいコンテスト</param>
        /// <returns>まとめられたコンテスト</returns>
        private List<Contest> SumContest(List<Contest> contests)
        {
            for (int i = 0; i < contests.Count; i++)
            {
                DateTime start = contests[i].StartTime;
                DateTime end = contests[i].EndTime;

                for (int j = i + 1; j < contests.Count; j++)
                {
                    if (!contests[j].Name.Contains(contests[i].Name) && !contests[i].Name.Contains(contests[j].Name))
                    {
                        if (start == contests[j].StartTime && end == contests[i].EndTime)
                        {
                            contests[i].Name += " / " + contests[j].Name;
                            contests[i].Link += "\r\n" + contests[j].Link;
                            contests.RemoveAt(j);
                        }
                    }
                }
            }

            return contests;
        }
    }
}