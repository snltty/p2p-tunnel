@echo off 

SET comment=%1

git tag -d v1.0.0.0
git push origin --delete v1.0.0.0

git add .
git commit -m "%comment%"
git tag -a v1.0.0.0 -m "v1.0"
git push origin dev
git push origin --tags