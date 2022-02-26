param ($pathspec = '*', $message = 'Update data (auto)')

git config user.name 'Workhorse'
git config user.email 'workhorse@knight.ru'
git add $pathspec
git commit -m $message
git push
