#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# LINKTAG RMPY001
"""Redmine CSV一括更新（既存チケット）

改善点（今回）
- カスタムフィールドが複数選択(multiple=true)の場合、CSVの値を分割して配列として送信
  例: "A, B" → ["A","B"]
- ユーザー型カスタムフィールドが複数選択の場合、
  プロジェクトメンバー(memberships)から name→id 変換し、配列のIDとして送信
  例: "User A, User B" → [<id_of_user_a>, <id_of_user_b>]

既存の改善点（前回まで）
- 日付(開始日/期日)を YYYY/MM/DD → YYYY-MM-DD に正規化して送信
- /users.json を使わず memberships を用いて担当者/ユーザー型CFを解決

注意
- Redmine公式ドキュメント上、/custom_fields.json は管理者権限が必要な場合があります。
  本スクリプトは取得できない場合でも動作しますが、その場合はカスタムフィールド更新を自動解決できません。
"""

from __future__ import annotations

import argparse
import csv
import json
import sys
import urllib.parse
import urllib.request
from dataclasses import dataclass
from typing import Any, Dict, List, Optional, Tuple, Set


def strip_(x: Any) -> str:
    return '' if x is None else str(x).strip()


def norm_date(x: Any) -> str:
    t = strip_(x)
    if not t:
        return ''
    return t.replace('/', '-')


def to_int(x: Any) -> Optional[int]:
    t = strip_(x)
    if not t:
        return None
    try:
        return int(float(t))
    except Exception:
        return None


def norm_bool_ja(x: Any) -> Optional[bool]:
    t = strip_(x)
    if not t:
        return None
    if t in ('はい','true','TRUE','1','Yes','YES'):
        return True
    if t in ('いいえ','false','FALSE','0','No','NO'):
        return False
    return None


def split_multi(v: str) -> List[str]:
    """複数選択のCSV値を分割。

    - 区切り: ',' '、' '，' ';' '；'
    - 空要素は除去
    """
    s = strip_(v)
    if not s:
        return []
    for sep in ['、', '，', ';', '；']:
        s = s.replace(sep, ',')
    parts = [p.strip() for p in s.split(',')]
    return [p for p in parts if p]


def http_json(method: str, url: str, api_key: str, payload: Optional[dict]=None):
    headers = {'X-Redmine-API-Key': api_key, 'Accept': 'application/json'}
    data = None
    if payload is not None:
        data = json.dumps(payload, ensure_ascii=False).encode('utf-8')
        headers['Content-Type'] = 'application/json'
    req = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req) as resp:
            text = resp.read().decode('utf-8', errors='replace')
            ct = resp.headers.get('Content-Type','')
            j = None
            if 'application/json' in ct and text.strip():
                try:
                    j = json.loads(text)
                except Exception:
                    j = None
            return resp.status, text, j
    except urllib.error.HTTPError as e:
        text = e.read().decode('utf-8', errors='replace')
        ct = e.headers.get('Content-Type','') if e.headers else ''
        j = None
        if 'application/json' in ct and text.strip():
            try:
                j = json.loads(text)
            except Exception:
                j = None
        return e.code, text, j


@dataclass
class ProjectInfo:
    id: int
    name: str
    identifier: str


@dataclass
class CustomFieldDef:
    id: int
    name: str
    multiple: bool
    field_format: str


@dataclass
class Lookup:
    status_by_name: Dict[str,int]
    priority_by_name: Dict[str,int]
    # name -> def
    custom_fields_by_name: Dict[str, CustomFieldDef]
    projects_by_name: Dict[str, ProjectInfo]
    assignees_by_project: Dict[str, Dict[str,int]]
    version_by_project_and_name: Dict[Tuple[str,str],int]
    category_by_project_and_name: Dict[Tuple[str,str],int]


def fetch_all(base_url: str, api_key: str, path: str, key: str, params: Optional[Dict[str,Any]]=None, limit: int=100):
    base = base_url.rstrip('/')
    items = []
    offset = 0
    params = dict(params or {})
    while True:
        q = dict(params)
        q['limit'] = limit
        q['offset'] = offset
        url = f"{base}/{path}.json?" + urllib.parse.urlencode(q)
        c,t,j = http_json('GET', url, api_key)
        if c >= 400 or not j or key not in j:
            break
        batch = j.get(key) or []
        items.extend(batch)
        if len(batch) < limit:
            break
        offset += limit
    return items


def build_lookups(base_url: str, api_key: str) -> Lookup:
    base = base_url.rstrip('/')

    status_by_name: Dict[str,int] = {}
    c,t,j = http_json('GET', f"{base}/issue_statuses.json", api_key)
    if j and 'issue_statuses' in j:
        for x in j['issue_statuses']:
            status_by_name[strip_(x.get('name'))] = int(x.get('id'))

    priority_by_name: Dict[str,int] = {}
    c,t,j = http_json('GET', f"{base}/enumerations/issue_priorities.json", api_key)
    if j and 'issue_priorities' in j:
        for x in j['issue_priorities']:
            priority_by_name[strip_(x.get('name'))] = int(x.get('id'))

    # custom fields definitions
    custom_fields_by_name: Dict[str, CustomFieldDef] = {}
    c,t,j = http_json('GET', f"{base}/custom_fields.json", api_key)
    if j and 'custom_fields' in j:
        for cf in j['custom_fields']:
            name = strip_(cf.get('name'))
            if not name:
                continue
            cfid = int(cf.get('id'))
            multiple = bool(cf.get('multiple'))
            field_format = strip_(cf.get('field_format'))
            custom_fields_by_name[name] = CustomFieldDef(cfid, name, multiple, field_format)

    projects = fetch_all(base_url, api_key, 'projects', 'projects')
    projects_by_name: Dict[str, ProjectInfo] = {}
    for p in projects:
        nm = strip_(p.get('name'))
        ident = strip_(p.get('identifier'))
        pid = p.get('id')
        if nm and ident and pid is not None:
            projects_by_name[nm] = ProjectInfo(int(pid), nm, ident)

    return Lookup(
        status_by_name=status_by_name,
        priority_by_name=priority_by_name,
        custom_fields_by_name=custom_fields_by_name,
        projects_by_name=projects_by_name,
        assignees_by_project={},
        version_by_project_and_name={},
        category_by_project_and_name={},
    )


def ensure_project_assignees(base_url: str, api_key: str, lookup: Lookup, project_identifier: str) -> None:
    if project_identifier in lookup.assignees_by_project:
        return
    memberships = fetch_all(base_url, api_key, f"projects/{urllib.parse.quote(project_identifier)}/memberships", 'memberships')
    m: Dict[str,int] = {}
    for mem in memberships:
        u = mem.get('user')
        g = mem.get('group')
        if isinstance(u, dict):
            nm = strip_(u.get('name'))
            uid = u.get('id')
            if nm and uid is not None:
                m[nm] = int(uid)
        if isinstance(g, dict):
            nm = strip_(g.get('name'))
            gid = g.get('id')
            if nm and gid is not None:
                m[nm] = int(gid)
    lookup.assignees_by_project[project_identifier] = m


def ensure_project_versions(base_url: str, api_key: str, lookup: Lookup, project_identifier: str) -> None:
    base = base_url.rstrip('/')
    url = f"{base}/projects/{urllib.parse.quote(project_identifier)}/versions.json?limit=100"
    c,t,j = http_json('GET', url, api_key)
    if j and 'versions' in j:
        for x in j['versions']:
            lookup.version_by_project_and_name[(project_identifier, strip_(x.get('name')))] = int(x.get('id'))


def ensure_project_categories(base_url: str, api_key: str, lookup: Lookup, project_identifier: str) -> None:
    base = base_url.rstrip('/')
    url = f"{base}/projects/{urllib.parse.quote(project_identifier)}/issue_categories.json"
    c,t,j = http_json('GET', url, api_key)
    if j and 'issue_categories' in j:
        for x in j['issue_categories']:
            lookup.category_by_project_and_name[(project_identifier, strip_(x.get('name')))] = int(x.get('id'))


def is_user_field(field_format: str) -> bool:
    return field_format == 'user'


def cf_multiple_hint(cfdef: Optional[CustomFieldDef]) -> bool:
    return bool(cfdef and cfdef.multiple)


def make_payload(row: Dict[str,str], lookup: Lookup, base_url: str, api_key: str, ignore_version: bool, warn: List[str]) -> Dict[str,Any]:
    issue: Dict[str,Any] = {}

    subj = strip_(row.get('題名'))
    if subj:
        issue['subject'] = subj

    desc = row.get('説明')
    if desc is not None and strip_(desc):
        issue['description'] = desc

    st = strip_(row.get('ステータス'))
    if st:
        sid = lookup.status_by_name.get(st)
        if sid is None:
            raise ValueError('ステータス名が見つかりません: ' + st)
        issue['status_id'] = sid

    pr = strip_(row.get('優先度'))
    if pr:
        pid = lookup.priority_by_name.get(pr)
        if pid is None:
            raise ValueError('優先度名が見つかりません: ' + pr)
        issue['priority_id'] = pid

    proj_name = strip_(row.get('プロジェクト'))
    pinfo = lookup.projects_by_name.get(proj_name) if proj_name else None

    assignee = strip_(row.get('担当者'))
    if assignee:
        if pinfo is None:
            raise ValueError('プロジェクト名が見つかりません(アクセス権不足の可能性): ' + proj_name)
        ensure_project_assignees(base_url, api_key, lookup, pinfo.identifier)
        amap = lookup.assignees_by_project.get(pinfo.identifier, {})
        uid = amap.get(assignee)
        if uid is None:
            raise ValueError("担当者名がプロジェクトメンバーに存在しません: '%s' (project=%s)" % (assignee, proj_name))
        issue['assigned_to_id'] = uid

    sd = norm_date(row.get('開始日'))
    if sd:
        issue['start_date'] = sd
    dd = norm_date(row.get('期日'))
    if dd:
        issue['due_date'] = dd

    dr = to_int(row.get('進捗率'))
    if dr is not None:
        issue['done_ratio'] = dr

    parent = to_int(row.get('親チケット'))
    if parent is not None:
        issue['parent_issue_id'] = parent

    priv = norm_bool_ja(row.get('プライベート'))
    if priv is not None:
        issue['is_private'] = priv

    # version/category
    if pinfo:
        ver = strip_(row.get('対象バージョン'))
        if ver:
            if (pinfo.identifier, ver) not in lookup.version_by_project_and_name:
                ensure_project_versions(base_url, api_key, lookup, pinfo.identifier)
            vid = lookup.version_by_project_and_name.get((pinfo.identifier, ver))
            if vid is None:
                if not ignore_version:
                    raise ValueError('対象バージョン名が見つかりません: ' + proj_name + ' ' + ver)
            else:
                issue['fixed_version_id'] = vid

        cat = strip_(row.get('カテゴリ'))
        if cat:
            if (pinfo.identifier, cat) not in lookup.category_by_project_and_name:
                ensure_project_categories(base_url, api_key, lookup, pinfo.identifier)
            cid = lookup.category_by_project_and_name.get((pinfo.identifier, cat))
            if cid is None:
                raise ValueError('カテゴリ名が見つかりません: ' + proj_name + ' ' + cat)
            issue['category_id'] = cid

    # custom fields
    standard = {
        '#','﻿#','プロジェクト','トラッカー','親チケット','親チケットの題名','ステータス','優先度','題名','作成者','担当者','更新日','カテゴリ','対象バージョン','開始日','期日',
        '予定工数','合計予定工数','作業時間','合計作業時間','進捗率','作成日','終了日','最終更新者','関連するチケット','ファイル','成果物ファイル名',
        'プライベート','説明','最新のコメント','変更理由・内容'
    }

    cfs = []
    for k,v in row.items():
        if k in standard:
            continue
        vv_raw = '' if v is None else str(v)
        vv = strip_(vv_raw)
        if not vv:
            continue

        cfdef = lookup.custom_fields_by_name.get(k)
        if cfdef is None:
            # 定義が取れない場合は名前一致しない可能性があるためスキップ
            continue

        multiple = cf_multiple_hint(cfdef)
        is_user = is_user_field(cfdef.field_format)

        if is_user:
            if pinfo is None:
                warn.append(f"CF '{k}' はユーザー型の可能性があるが、プロジェクト特定できずスキップ")
                continue
            ensure_project_assignees(base_url, api_key, lookup, pinfo.identifier)
            amap = lookup.assignees_by_project.get(pinfo.identifier, {})

            if multiple:
                names = split_multi(vv_raw)
                ids: List[int] = []
                missing: List[str] = []
                for nm in names:
                    uid = amap.get(nm)
                    if uid is None:
                        missing.append(nm)
                    else:
                        ids.append(uid)
                if missing:
                    warn.append(f"CF '{k}' の値 {missing} がメンバー一覧にないためスキップ")
                    continue
                cfs.append({'id': cfdef.id, 'value': ids})
            else:
                uid = amap.get(vv)
                if uid is None:
                    warn.append(f"CF '{k}' の値 '{vv}' がメンバー一覧にないためスキップ")
                    continue
                cfs.append({'id': cfdef.id, 'value': uid})
            continue

        # 非ユーザー型
        if multiple:
            vals = split_multi(vv_raw)
            if not vals:
                continue
            cfs.append({'id': cfdef.id, 'value': vals})
        else:
            cfs.append({'id': cfdef.id, 'value': vv})

    if cfs:
        issue['custom_fields'] = cfs

    return {'issue': issue}


def parse_args(argv: List[str]):
    p = argparse.ArgumentParser()
    p.add_argument('--base-url', required=True)
    p.add_argument('--api-key', required=True)
    p.add_argument('--csv', required=True)
    g = p.add_mutually_exclusive_group(required=True)
    g.add_argument('--dry-run', action='store_true')
    g.add_argument('--apply', action='store_true')
    p.add_argument('--encoding', default='utf-8-sig')
    p.add_argument('--ignore-version', action='store_true')
    p.add_argument('--only-ids', default='')
    return p.parse_args(argv)


def parse_only_ids(s: str) -> Set[int]:
    s = strip_(s)
    if not s:
        return set()
    out: Set[int] = set()
    for part in s.split(','):
        v = to_int(part)
        if v is not None:
            out.add(v)
    return out


def main(argv: List[str]) -> int:
    args = parse_args(argv)
    base = args.base_url.rstrip('/')

    c,t,j = http_json('GET', f"{base}/projects.json?limit=1", args.api_key)
    if c >= 400:
        print('[ERROR] Redmine接続に失敗 HTTP=', c)
        print(t[:1000])
        return 2

    only = parse_only_ids(args.only_ids)
    lookup = build_lookups(base, args.api_key)

    total = ok = ng = 0
    with open(args.csv, 'r', encoding=args.encoding, newline='') as f:
        reader = csv.DictReader(f)
        if not reader.fieldnames:
            print('[ERROR] CSVヘッダが読めません')
            return 2
        id_key = '#' if '#' in reader.fieldnames else ('﻿#' if '﻿#' in reader.fieldnames else None)
        if id_key is None:
            print('[ERROR] CSVヘッダに # がありません')
            print(reader.fieldnames)
            return 2

        for row in reader:
            issue_id = to_int(row.get(id_key))
            if issue_id is None:
                continue
            if only and issue_id not in only:
                continue

            total += 1
            warn: List[str] = []
            try:
                payload = make_payload(row, lookup, base, args.api_key, args.ignore_version, warn)
            except Exception as e:
                ng += 1
                print('[NG] #' + str(issue_id) + ' ' + str(e))
                continue

            for w in warn:
                print('[WARN] #' + str(issue_id) + ' ' + w)

            if not payload['issue']:
                print('[SKIP] #' + str(issue_id) + ' 更新対象が空')
                continue

            if args.dry_run:
                print('[DRY] #' + str(issue_id) + ' ' + json.dumps(payload, ensure_ascii=False))
                ok += 1
                continue

            c3,t3,j3 = http_json('PUT', f"{base}/issues/{issue_id}.json", args.api_key, payload)
            if 200 <= c3 < 300:
                ok += 1
                print('[OK] #' + str(issue_id))
            else:
                ng += 1
                print('[NG] #' + str(issue_id) + ' HTTP=' + str(c3))
                print(t3[:2000])

    print('[SUMMARY] total=%d ok=%d ng=%d' % (total, ok, ng))
    return 0 if ng == 0 else 1


if __name__ == '__main__':
    raise SystemExit(main(sys.argv[1:]))
