// This source code is a part of Custom Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.
// File: dcinside.js - DCInside Script

register("dcinside", "rollrat", "1.0", "dcinside");

info("DCInside Support Script V1.0");

function parse_minor_gallery_board(html) {
  var result = [];
  var nn = html.select('/html[1]/body[1]/div[2]/div[2]/main[1]/section[1]/article[2]/div[2]/table[1]/tbody[1]');
  for (var i = 1; ; i++) {
    var node = html.select_single("/html[1]/body[1]/div[2]/div[2]/main[1]/section[1]/article[2]/div[2]/table[1]/tbody[1]/tr[" + i + "]");
    if (!node) break;
    var item = {};
    item['number'] = node.select_single('./td[1]').inner_text;
    item['prefix'] = node.select_single('./td[2]').inner_text;
    item['icon'] = node.select_single('./td[3]/a[1]/em[1]').attr('class');
    item['title'] = node.select_single('./td[3]/a[1]').inner_text;
    if (node.select_single('./td[3]/a[2]/span[1]'))
      item['comment'] = node.select_single('./td[3]/a[2]/span[1]').inner_text;
    item['author'] = node.select_single('./td[4]').inner_text.trim();
    item['write'] = node.select_single('./td[5]').inner_text;
    item['views'] = node.select_single('./td[6]').inner_text;
    item['upvote'] = node.select_single('./td[7]').inner_text;

    if (item['views'].trim() == '-')
      continue;
    if (item['icon'].split(' ').includes('icon_notice'))
      continue;

    result.push(item);
  }
  return result;
}

function parse_gallery_board(html) {

}

//info(x.inner_text);
//var x = net.get_string('https://google.com');

//info(net.get_string('https://google.com'));

var node = net.get_html('https://gall.dcinside.com/mgallery/board/lists?id=jusik');
debug('start');
for (var i = 0; i < 100; i++) {
  var x = parse_minor_gallery_board(node);
}
debug('end')
// var task = net.get_task('asdf', {
//   'asdf': 'asdfxvc'
// });