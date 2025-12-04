# -*- coding: utf-8 -*-
from pathlib import Path
path = Path("src/views/workbench/clientVersion/channel.vue")
text = path.read_text(encoding="utf-8")
text = text.replace('label="OSS Bucket"', 'label="对象存储 Bucket"')
text = text.replace('留空则使用全局 OSS 配置的 Bucket', '留空则使用全局对象存储配置的 Bucket')
text = text.replace('未找到任何 AppVersion，请确认 OSS 目录中存在版本文件夹', '未找到任何 AppVersion，请确认对象存储目录中存在版本文件夹')
path.write_text(text, encoding="utf-8")
