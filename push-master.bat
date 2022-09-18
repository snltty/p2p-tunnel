@echo off 

git tag -d v1.0.0.0
git push origin --delete v1.0.0.0

git checkout master
git reset --hard origin/dev
git pull
git add .

git tag -a v1.0.0.0 -m "v1.0"
git push origin master
git push origin --tags
git checkout dev