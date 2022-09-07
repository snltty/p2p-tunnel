@echo off 

git checkout master
git reset --hard origin/dev
git pull
git add .
git push origin master
git checkout dev