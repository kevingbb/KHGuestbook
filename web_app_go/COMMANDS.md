# For testing purposes
docker build -t guestbook-web:go .
docker run -d -e "SQLSERVER=ip.address" -e "SQLPORT=10433" -e "SQLID=sa" -e "SQLPWD=Your@Password" -e "SQLDB=sql_guestbook" --name web -p 80:8001 guestbook-web:go